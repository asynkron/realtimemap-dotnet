using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Backend.Actors;
using Backend.Models;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Proto;
using Proto.Cluster;
using Channel = System.Threading.Channels.Channel;

namespace Backend.Services
{
    public class MapBackendService : MapBackend.MapBackendBase
    {
        private readonly Cluster _cluster;
        private readonly ILogger<MapBackendService> _logger;
        private readonly ActorSystem _system;

        public MapBackendService(ILogger<MapBackendService> logger, Cluster cluster)
        {
            _logger = logger;
            _cluster = cluster;
            _system = cluster.System;
        }

        public override async Task Connect(IAsyncStreamReader<CommandEnvelope> requestStream,
            IServerStreamWriter<PositionBatch> responseStream, ServerCallContext context)
        {
            //this is a channel for all events for this specific request
            var positionsChannel = Channel.CreateUnbounded<Position>();
            var props = Props.FromProducer(() => new ViewportActor(positionsChannel));

            var helsinkiAirportLocation = new GeoPoint(24.96907, 60.31146);
            var geofenceProps = Props.FromProducer(() => new GeofenceActor("Helsinki Airport", new CircularGeofence(helsinkiAirportLocation, 5000)));
            
            //this is out viewport actor for this request
            var viewportPid = _cluster.System.Root.Spawn(props);
            var geofencePid = _cluster.System.Root.Spawn(geofenceProps);

            //subscribe to all position events, so that our viewport actor receives all those positions
            var sub = _system.EventStream.Subscribe<Position>(_system.Root, viewportPid, geofencePid);

            //create a pipeline that reads from the position channel
            //buffers the positions up to X positions
            //translate those buffers into PositionBatch messages
            //write those to the response stream
            //
            //why batching? it generally keeps buffers saturated and cause less starts/stops
            //this example would work without it
            _ =
                positionsChannel
                    .Reader
                    .ReadAllAsync()
                    .Buffer(10)
                    .Select(x => new PositionBatch
                    {
                        Positions = {x}
                    })
                    .ForEachAwaitAsync(responseStream.WriteAsync);

            try
            {
                //keep this method alive for as long as the client is connected
                await foreach (var x in requestStream.ReadAllAsync())
                {
                    if (x.CommandCase == CommandEnvelope.CommandOneofCase.UpdateViewport)
                    {
                        _cluster.System.Root.Send(viewportPid, x.UpdateViewport);
                    }
                    //consume incoming commands here
                }
            }
            finally
            {
                //clean up all resources
                _logger.LogWarning("Request ended...");
                positionsChannel.Writer.Complete();
                sub.Unsubscribe();
                await _cluster.System.Root.StopAsync(viewportPid);
                await _cluster.System.Root.StopAsync(geofencePid);
            }
        }

        public override async Task<GetTrailResponse> GetTrail(GetTrailRequest request, ServerCallContext context)
        {
            var trail = await _cluster
                .GetVehicleActor(request.AssetId).GetPositionsHistory(new GetPositionsHistoryRequest(), CancellationToken.None);

            return new GetTrailResponse
            {
                PositionBatch = trail
            };
        }
    }
}