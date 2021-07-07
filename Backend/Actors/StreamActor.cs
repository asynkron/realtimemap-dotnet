using System.Threading.Tasks;
using Grpc.Core;
using Proto;

namespace Backend.Actors
{
    public class StreamActor : IActor
    {
        private readonly IServerStreamWriter<PositionBatch> _streamWriter;

        public StreamActor(IServerStreamWriter<PositionBatch> streamWriter)
        {
            _streamWriter = streamWriter;
        }
        
        public async Task ReceiveAsync(IContext context)
        {
            if (context.Message is PositionBatch batch)
            {
                await _streamWriter.WriteAsync(batch);
            }
        }
    }
}