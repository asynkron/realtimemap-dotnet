using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        public override Task Connect(IAsyncStreamReader<CommandEnvelope> requestStream, IServerStreamWriter<PositionBatch> responseStream, ServerCallContext context)
        {
            return base.Connect(requestStream, responseStream, context);
        }
    }
}