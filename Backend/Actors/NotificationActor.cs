using System.Threading.Tasks;
using Grpc.Core;
using Proto;

namespace Backend.Actors
{
    public class NotificationActor : IActor
    {
        private readonly IServerStreamWriter<ResponseEnvelope> _serverStreamWriter;

        public NotificationActor(IServerStreamWriter<ResponseEnvelope> serverStreamWriter)
        {
            _serverStreamWriter = serverStreamWriter;
        }
        
        public async Task ReceiveAsync(IContext context)
        {
            switch (context.Message)
            {
                case Notification notification:
                {
                    await _serverStreamWriter.WriteAsync(new ResponseEnvelope
                    {
                        Notification = notification
                    });
                    break;
                }
            }
        }
    }
}