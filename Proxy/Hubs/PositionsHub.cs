using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Backend;
using Grpc.Core;
using JetBrains.Annotations;
using Microsoft.AspNetCore.SignalR;
using Proxy.DTO;

namespace Proxy.Hubs
{
    class PositionHubState
    {
        public MapBackend.MapBackendClient Client;
        public AsyncDuplexStreamingCall<CommandEnvelope, ResponseEnvelope> GrpcConnection;
    }

    [PublicAPI]
    public class PositionsHub : Hub
    {
        public string ConnectionId => $"connection{Context.ConnectionId}";

        private PositionHubState State
        {
            get
            {
                if (!Context.Items.ContainsKey("state")) Context.Items.Add("state", new PositionHubState());

                return Context.Items["state"] as PositionHubState;
            }
        }

        public async IAsyncEnumerable<HubMessageDto> Connect()
        {
            Console.WriteLine("Connect user session " + ConnectionId);

            var channel =
                new Channel("127.0.0.1", 5002,
                    ChannelCredentials.Insecure); //GrpcChannel.ForAddress(new Uri("https://localhost:4040"));

            Console.WriteLine("Got response");

            var grpcClient = new MapBackend.MapBackendClient(channel);
            var conn = grpcClient.Connect();
            
            State.Client = grpcClient;
            State.GrpcConnection = conn;

            Console.WriteLine("Got response...");

            CommandEnvelope envelope = new()
            {
                CreateViewport = new CreateViewport()
                {
                    ConnectionId = ConnectionId
                }
            };

            await conn.RequestStream.WriteAsync(envelope);

            Console.WriteLine($"Connected {envelope}");

            var responseStream = State.GrpcConnection.ResponseStream;

            await foreach (var grpcResponse in responseStream.ReadAllAsync())
            {
                switch (grpcResponse.ResponseCase)
                {
                        case ResponseEnvelope.ResponseOneofCase.Notification:
                        {
                            var res = new HubMessageDto(HubMessageType.Notification, new NotificationDto
                            {
                                Message = grpcResponse.Notification.Message
                            });

                            yield return res;

                            continue;
                        }

                        case ResponseEnvelope.ResponseOneofCase.PositionBatch:
                        {
                            if (!grpcResponse.PositionBatch.Positions.Any())
                                continue;

                            var dtoBatch = grpcResponse.PositionBatch.Positions.Select(PositionDto.MapFrom).ToArray();

                            var res = new HubMessageDto(HubMessageType.Position, new PositionsDto
                            {
                                Positions = dtoBatch,
                            });

                            yield return res;

                            continue;
                        }
                }

            }
        }

        public async Task SetViewport(double swLng, double swLat, double neLng, double neLat)
        {
            CommandEnvelope envelope = new()
            {
                UpdateViewport = new UpdateViewport
                {
                    Viewport = new Viewport
                    {
                        SouthWest = new GeoPoint(swLng, swLat),
                        NorthEast = new GeoPoint(neLng, neLat)
                    }
                }
            };

            await State.GrpcConnection.RequestStream.WriteAsync(envelope);
        }
    }
}