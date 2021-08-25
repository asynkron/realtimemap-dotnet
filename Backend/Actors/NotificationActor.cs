using System.Threading.Channels;
using System.Threading.Tasks;
using Proto;

namespace Backend.Actors
{
    public class NotificationActor : IActor
    {
        private readonly ChannelWriter<Notification> _notificationWriter;

        public NotificationActor(ChannelWriter<Notification> notificationWriter)
        {
            _notificationWriter = notificationWriter;
        }

        public async Task ReceiveAsync(IContext context)
        {
            switch (context.Message)
            {
                case Notification notification:
                    await _notificationWriter.WriteAsync(notification);
                    break;
            }
        }
    }
}