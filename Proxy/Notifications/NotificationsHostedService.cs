using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Backend;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Proxy.Hubs;

namespace Proxy.Notifications
{
    public class NotificationsHostedService : BackgroundService
    {
        private readonly IHubContext<PositionsHub> _positionsHub;
        
        private AsyncServerStreamingCall<Notification> _streamingCall;

        public NotificationsHostedService(IHubContext<PositionsHub> positionsHub)
        {
            _positionsHub = positionsHub;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine($"Starting {nameof(NotificationsHostedService)}...");
            
            var channel = new Channel(
                host: "127.0.0.1",
                port: 5002,
                credentials: ChannelCredentials.Insecure);

            var grpcClient = new NotificationBackend.NotificationBackendClient(channel);

            _streamingCall = grpcClient.Connect(
                request: new Empty(),
                cancellationToken: stoppingToken);

            var responseStream = _streamingCall.ResponseStream;
            
            await responseStream
                .ReadAllAsync(stoppingToken)
                .ForEachAwaitAsync(HandleNotification, stoppingToken);
        }

        private async Task HandleNotification(Notification notification)
        {
            Console.WriteLine($"Received notification: {notification.Message}");

            await _positionsHub.Clients.All.SendAsync("notification", notification.Message);
        }
    }
}