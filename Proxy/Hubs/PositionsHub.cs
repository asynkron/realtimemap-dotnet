using System;
using System.Collections.Generic;
using System.Linq;
using Backend;
using Grpc.Core;
using JetBrains.Annotations;
using Microsoft.AspNetCore.SignalR;

namespace Proxy.Hubs
{
    class PositionHubState
    {
        public AsyncDuplexStreamingCall<CommandEnvelope, PositionBatch> GrpcConnection;
    }

    [PublicAPI]
    public class PositionsHub : Hub
    {
        public string ConnectionId => $"connection{Context.ConnectionId}";

        private PositionHubState State {
            get {
                if (!Context.Items.ContainsKey("state")) Context.Items.Add("state", new PositionHubState());

                return Context.Items["state"] as PositionHubState;
            }
        }

        public async IAsyncEnumerable<PositionDto[]> Connect()
        {
            Console.WriteLine("Connect user session " + ConnectionId);

            var channel =
                new Channel("127.0.0.1", 4040, ChannelCredentials.Insecure); //GrpcChannel.ForAddress(new Uri("https://localhost:4040"));
            
            var grpcClient = new MapBackend.MapBackendClient(channel);
            var conn = grpcClient.Connect();
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

            await foreach (var positionBatch in responseStream.ReadAllAsync())
            {
                if (!positionBatch.Positions.Any())
                    continue;

                var dtoBatch = positionBatch.Positions.Select(MapPositionDto).ToArray();
                yield return dtoBatch;
            }
        }

        private static PositionDto MapPositionDto(Position position) =>
            new()
            {
                Latitude = position.Latitude,
                Longitude = position.Longitude,
                Timestamp = position.Timestamp,
                Heading = position.Heading,
                // Speed = position.Speed
            };
    }
}