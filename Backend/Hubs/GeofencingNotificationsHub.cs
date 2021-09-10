using System;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.AspNetCore.SignalR;
using Proto;
using Proto.Cluster;

namespace Backend.Hubs
{
    [PublicAPI]
    public class GeofencingNotificationsHub : Hub
    {
        private readonly ActorSystem _actorSystem;
        private readonly Cluster _cluster;

        public GeofencingNotificationsHub(ActorSystem actorSystem, Cluster cluster)
        {
            _actorSystem = actorSystem;
            _cluster = cluster;
        }
        
        private HubState State
        {
            get => Context.Items["state"] as HubState;
            set => Context.Items["state"] = value;
        }
        
        private class HubState
        {
            public EventStreamSubscription<object> NotificationsSubscription;
        }

        public override Task OnConnectedAsync()
        {
            State = new HubState();
            
            Console.WriteLine("Subscribing to positions subscription");

            var notificationsChannel = Channel.CreateUnbounded<Notification>();
            
            // subscribe to all notification events
            State.NotificationsSubscription = _actorSystem.EventStream.Subscribe<Notification>(
                async notification => await notificationsChannel.Writer.WriteAsync(notification)
            );
            
            // since this hub will be disposed later on, we need to keep a reference to a calling client
            var caller = Clients.Caller;
            
            // this pipeline will read notifications from the channel and send them to SignalR client
            _ = notificationsChannel
                .Reader
                .ReadAllAsync()
                .ForEachAwaitAsync(async notification =>
                {
                    await caller.SendAsync("notification", notification.Message);
                });

            return Task.CompletedTask;
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            Console.WriteLine("Unsubscribing from positions subscription");

            State.NotificationsSubscription?.Unsubscribe();

            return Task.CompletedTask;
        }
    }
}