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
            _ = StartConsumer();
        }

        private async Task StartConsumer()
        {
            await Task.Yield();
            var positions = await MqttIngress.Start();
            var batches = positions.Buffer(100);
            await foreach (var batch in batches)
            {
                Console.WriteLine("Got batch from mqtt" + batch.Count);
                var requests = batch
                    .Select(m => _cluster
                        .GetVehicleActor(m.VehicleId).OnPosition(m, CancellationTokens.FromSeconds(1)))
                    .ToList();

                await Task.WhenAll(requests);
            }
        }

        public override async Task Connect(IAsyncStreamReader<CommandEnvelope> requestStream, IServerStreamWriter<PositionBatch> responseStream, ServerCallContext context)
        {
            var positionsChannel = System.Threading.Channels.Channel.CreateUnbounded<Position>();
            var props = Props.FromProducer(() => new StreamActor(positionsChannel));
            var streamPid = _cluster.System.Root.Spawn(props);
            var sub = _system.EventStream.Subscribe<Position>(_system.Root, streamPid);

            // _ = Task.Run(async () =>
            // {
            //     var batches = 
            //         positionsChannel
            //         .Reader
            //         .ReadAllAsync()
            //         .Buffer(100)
            //         .Select(x => new PositionBatch
            //         {
            //             Positions = {x}
            //         });
            //
            //     await foreach (var batch in batches)
            //     {
            //         await responseStream.WriteAsync(batch);
            //     }
            // });
            
            try
            {
                await foreach (var x in requestStream.ReadAllAsync())
                {
                    //consume incoming commands here
                }
            }
            finally
            {
                _logger.LogWarning("Request ended...");
                positionsChannel.Writer.Complete();
                sub.Unsubscribe();
                await _cluster.System.Root.StopAsync(streamPid);
            }
        }
    }
}