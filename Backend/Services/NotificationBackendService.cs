using System.Linq;
using System.Threading.Tasks;
using Backend.Actors;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Proto;
using Proto.Cluster;
using Channel = System.Threading.Channels.Channel;

namespace Backend.Services
{
    public class NotificationBackendService : NotificationBackend.NotificationBackendBase
    {
        private readonly Cluster _cluster;
        private readonly ILogger<NotificationBackendService> _logger;
        private readonly ActorSystem _system;

        public NotificationBackendService(
            Cluster cluster,
            ILogger<NotificationBackendService> logger,
            ActorSystem system)
        {
            _cluster = cluster;
            _logger = logger;
            _system = system;
        }

        public override async Task Connect(Empty request, IServerStreamWriter<Notification> responseStream, ServerCallContext context)
        {
            _logger.LogWarning("NotificationService/Connect request started");
            
            var notificationsChannel = Channel.CreateUnbounded<Notification>();
            
            var notificationActorProps = Props.FromProducer(() => new NotificationActor(notificationsChannel.Writer));
        
            var notificationActorPid = _cluster.System.Root.Spawn(notificationActorProps);

            var notificationsSubscription = _system.EventStream.Subscribe<Notification>(_system.Root, notificationActorPid);

            try
            {
                await notificationsChannel
                    .Reader
                    .ReadAllAsync()
                    .ForEachAwaitAsync(responseStream.WriteAsync);
            }
            finally
            {
                _logger.LogWarning("NotificationService/Connect request ended");
                
                notificationsChannel.Writer.Complete();
                
                notificationsSubscription.Unsubscribe();
                
                await _cluster.System.Root.StopAsync(notificationActorPid);
            }
        }
    }
}