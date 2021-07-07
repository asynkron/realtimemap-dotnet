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

        public MapBackendService(ILogger<MapBackendService> logger, Cluster cluster)
        {
            _logger = logger;
            _cluster = cluster;
            _ = StartConsumer();
        }

        private async Task StartConsumer()
        {
            await Task.Yield();
            var positions = await MqttIngress.Start();
            var batches = positions.Buffer(100);
            await foreach (var batch in batches)
            {
                var requests = batch
                    .Select(m => _cluster
                        .GetVehicleActor(m.VehicleId).OnPosition(m, CancellationTokens.FromSeconds(1)))
                    .ToList();

                await Task.WhenAll(requests);
            }
        }

        public override async Task Connect(IAsyncStreamReader<CommandEnvelope> requestStream, IServerStreamWriter<PositionBatch> responseStream, ServerCallContext context)
        {
            var props = Props.FromProducer(() => new StreamActor(responseStream));
            var streamPid = _cluster.System.Root.Spawn(props);
            try
            {
                await foreach (var x in requestStream.ReadAllAsync())
                {
                    //consume incoming commands here
                }
            }
            finally
            {
                await _cluster.System.Root.StopAsync(streamPid);
            }
        }
    }
}