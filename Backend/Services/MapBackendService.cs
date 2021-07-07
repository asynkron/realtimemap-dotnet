using System.Linq;
using System.Threading.Tasks;
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
        }

        public override async Task Connect(IAsyncStreamReader<CommandEnvelope> requestStream, IServerStreamWriter<PositionBatch> responseStream, ServerCallContext context)
        {
            var positions = await MqttIngress.Start();

            var batches = positions.Buffer(10);
            await foreach (var batch in batches)
            {
                var requests = batch
                    .Select(m => _cluster
                        .GetVehicleActor(m.VehicleId).OnPosition(m, CancellationTokens.FromSeconds(1)))
                    .ToList();

                await Task.WhenAll(requests);
                
                var pb = new PositionBatch()
                {
                    Positions = {batch}
                };
                await responseStream.WriteAsync(pb);
            }
        }
    }
}