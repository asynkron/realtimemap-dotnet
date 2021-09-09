using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
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
    class PositionHubState
    {
        public PID ViewportPid;
        public EventStreamSubscription<object> Subscription;
    }

    [PublicAPI]
    public class PositionsHub : Hub
    {
        private readonly ActorSystem _system;
        private readonly Cluster _cluster;

        public PositionsHub(ActorSystem system, Cluster cluster)
        {
            _system = system;
            _cluster = cluster;
        }
        
        public string ConnectionId => $"connection{Context.ConnectionId}";

        private PositionHubState State
        {
            get
            {
                if (!Context.Items.ContainsKey("state")) Context.Items.Add("state", new PositionHubState());

                return Context.Items["state"] as PositionHubState;
            }
        }
        
        public async IAsyncEnumerable<PositionsDto> Connect([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            Console.WriteLine("Subscribing to positions subscription");
            
            // this is a channel for all events for this specific request
            var positionsChannel = Channel.CreateUnbounded<Position>();
            
            // this is out viewport actor for this request
            var props = Props.FromProducer(() => new ViewportActor(positionsChannel));
            State.ViewportPid = _cluster.System.Root.Spawn(props);

            // subscribe to all position events, so that our viewport actor receives all those positions
            State.Subscription = _system.EventStream.Subscribe<Position>(_system.Root, State.ViewportPid);

            //create a pipeline that reads from the position channel
            //buffers the positions up to X positions
            //translate those buffers into PositionBatch messages
            //write those to the response stream
            //
            //why batching? it generally keeps buffers saturated and cause less starts/stops
            //this example would work without it
            var positionBatches = positionsChannel
                .Reader
                .ReadAllAsync(cancellationToken)
                .Buffer(10);

            await foreach (var positionBatch in positionBatches.WithCancellation(cancellationToken))
            {
                yield return new PositionsDto
                {
                    Positions = positionBatch
                        .Select(PositionDto.MapFrom)
                        .ToArray()
                };
            }
        }

        public Task SetViewport(double swLng, double swLat, double neLng, double neLat)
        {
            _cluster.System.Root.Send(State.ViewportPid, new UpdateViewport
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
            
            State.Subscription?.Unsubscribe();

            if (State.ViewportPid is not null)
            {
                await _cluster.System.Root.StopAsync(State.ViewportPid);
            }
        }
    }
}