using System;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Backend.Actors;
using Backend.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Proto;
using Proto.Cluster;

namespace Backend.Notifications
{
    public class NotificationsHostedService : BackgroundService
    {
        
        private readonly IHubContext<PositionsHub> _positionsHub;
        private readonly Cluster _cluster;
        private readonly ILogger<NotificationsHostedService> _logger;
        private readonly ActorSystem _actorSystem;

        public NotificationsHostedService(
            IHubContext<PositionsHub> positionsHub,
            Cluster cluster,
            ILogger<NotificationsHostedService> logger,
            ActorSystem actorSystem)
        {
            _positionsHub = positionsHub;
            _cluster = cluster;
            _logger = logger;
            _actorSystem = actorSystem;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine($"Starting {nameof(NotificationsHostedService)}...");

            var notificationsChannel = Channel.CreateUnbounded<Notification>();
            
            var notificationActorProps = Props.FromProducer(() => new NotificationActor(notificationsChannel.Writer));
        
            var notificationActorPid = _cluster.System.Root.Spawn(notificationActorProps);

            var notificationsSubscription = _actorSystem.EventStream.Subscribe<Notification>(_actorSystem.Root, notificationActorPid);

            try
            {
                await notificationsChannel
                    .Reader
                    .ReadAllAsync(stoppingToken)
                    .ForEachAwaitAsync(HandleNotification, stoppingToken);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Handling notifications failed.");
            }
            finally
            {
                Console.WriteLine($"Stopping {nameof(NotificationsHostedService)}...");
                
                notificationsChannel.Writer.Complete();
                
                notificationsSubscription.Unsubscribe();
                
                await _cluster.System.Root.StopAsync(notificationActorPid);
            }
        }

        private async Task HandleNotification(Notification notification)
        {
            Console.WriteLine($"Received notification: {notification.Message}");

            await _positionsHub.Clients.All.SendAsync("notification", notification.Message);
        }
    }
}