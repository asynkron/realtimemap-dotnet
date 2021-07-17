using System;
using System.Linq;
using System.Threading.Tasks;
using Backend.Actors;
using Backend.MQTT;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Proto;
using Proto.Cluster;

namespace Backend.Services
{
    public class MapBackendService : MapBackend.MapBackendBase
    {
        private readonly ILogger<MapBackendService> _logger;
        private readonly Cluster _cluster;
        private readonly ActorSystem _system;

        public MapBackendService(ILogger<MapBackendService> logger, Cluster cluster)
        {
            _logger = logger;
            _cluster = cluster;
            _system = cluster.System;
            
            //TODO: this should be moved to elsewhere
            _ = StartConsumer();
        }

        //TODO: this should be moved to elsewhere
        private async Task StartConsumer()
        {
            await Task.Yield();
            var positions = await MqttIngress.Start();
            var batches = positions.Buffer(100);
            await foreach (var batch in batches)
            {
                try
                {
                    var requests = batch
                        .Select(m => _cluster
                            .GetVehicleActor(m.VehicleId).OnPosition(m, CancellationTokens.FromSeconds(1)))
                        .ToList();

                    await Task.WhenAll(requests);
                }
                catch(Exception x)
                {
                    //There is a bug in the typed client atm, we are getting not supported exception sometimes
                    _logger.LogError(x, "Mqtt reader failed");
                }
            }
        }

        public override async Task Connect(IAsyncStreamReader<CommandEnvelope> requestStream, IServerStreamWriter<PositionBatch> responseStream, ServerCallContext context)
        {
            //this is a channel for all events for this specific request
            var positionsChannel = System.Threading.Channels.Channel.CreateUnbounded<Position>();
            var props = Props.FromProducer(() => new ViewportActor(positionsChannel));
            //this is out viewport actor for this request
            var viewportPid = _cluster.System.Root.Spawn(props);
            
            //subscribe to all position events, so that our viewport actor receives all those positions
            var sub = _system.EventStream.Subscribe<Position>(_system.Root, viewportPid);

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
                    .Buffer(100)
                    .Select(x => new PositionBatch
                    {
                        Positions = {x}
                    }).ForEachAwaitAsync(responseStream.WriteAsync);

            try
            {
                //keep this method alive for as long as the client is connected
                await foreach (var x in requestStream.ReadAllAsync())
                {
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
            }
        }
    }
}