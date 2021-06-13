using System.Linq;
using System.Threading.Tasks;
using Backend.MQTT;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace Backend
{
    public class MapBackendService : MapBackend.MapBackendBase
    {
        private readonly ILogger<MapBackendService> _logger;

        public MapBackendService(ILogger<MapBackendService> logger)
        {
            _logger = logger;
        }

        public override async Task Connect(IAsyncStreamReader<CommandEnvelope> requestStream, IServerStreamWriter<PositionBatch> responseStream, ServerCallContext context)
        {
            var positions = await MqttIngress.Start();

            var batches = positions.Buffer(100);
            await foreach (var batch in batches)
            {
                var pb = new PositionBatch()
                {
                    Positions = {batch}
                };
                await responseStream.WriteAsync(pb);
            }
        }
    }
}