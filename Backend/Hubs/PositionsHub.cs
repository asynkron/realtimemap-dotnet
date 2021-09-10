using System;
using System.Linq;
using System.Threading.Tasks;
using Backend.Actors;
using Backend.DTO;
using JetBrains.Annotations;
using Microsoft.AspNetCore.SignalR;
using Proto;
using Proto.Cluster;
using Channel = System.Threading.Channels.Channel;

namespace Backend.Hubs
{
    [PublicAPI]
    public class PositionsHub : Hub
    {
        private readonly ActorSystem _actorSystem;
        private readonly Cluster _cluster;

        public PositionsHub(ActorSystem actorSystem, Cluster cluster)
        {
            _actorSystem = actorSystem;
            _cluster = cluster;
        }
        
        private HubState State
        {
            get => Context.Items["state"] as HubState;
            set => Context.Items["state"] = value;
        }
        
        class HubState
        {
            public PID ViewportActorPid;
            public EventStreamSubscription<object> ViewportSubscription;
        }

        public override Task OnConnectedAsync()
        {
            State = new HubState();
            
            Console.WriteLine("Subscribing to positions subscription");
            
            SubscribeToViewportPositions();

            return Task.CompletedTask;
        }

        private void SubscribeToViewportPositions()
        {
            var positionsChannel = Channel.CreateUnbounded<Position>();

            // viewport actor will receive position updates and write
            // the ones inside configured bounds to the channel
            State.ViewportActorPid = _cluster.System.Root.Spawn(
                Props.FromProducer(() => new ViewportActor(positionsChannel.Writer))
            );

            // subscribe viewport actor to all position events
            State.ViewportSubscription = _actorSystem.EventStream.Subscribe<Position>(
                _actorSystem.Root,
                State.ViewportActorPid
            );

            // since this hub will be disposed later on, we need to keep a reference to a calling client
            var caller = Clients.Caller;
            
            // this pipeline will read positions from the channel and send them to SignalR client in batches
            // batching is used to keep buffers saturated and to cause less starts/stops
            _ = positionsChannel
                .Reader
                .ReadAllAsync()
                .Buffer(10)
                .ForEachAwaitAsync(async positionBatch =>
                {
                    await caller.SendAsync("positions", new PositionsDto
                    {
                        Positions = positionBatch
                            .Select(PositionDto.MapFrom)
                            .ToArray()
                    });
                });
        }

        public Task SetViewport(double swLng, double swLat, double neLng, double neLat)
        {
            Console.WriteLine("Setting viewport");
            
            _cluster.System.Root.Send(State.ViewportActorPid, new UpdateViewport
            {
                Viewport = new Viewport
                {
                    SouthWest = new GeoPoint(swLng, swLat),
                    NorthEast = new GeoPoint(neLng, neLat)
                }
            });

            return Task.CompletedTask;
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            Console.WriteLine("Unsubscribing from positions subscription");

            State.ViewportSubscription?.Unsubscribe();

            if (State.ViewportActorPid is not null)
            {
                await _cluster.System.Root.StopAsync(State.ViewportActorPid);
            }
        }
    }
}