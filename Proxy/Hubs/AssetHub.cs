using System;
using System.Collections.Generic;
using System.Linq;
using Backend;
using Grpc.Core;
using JetBrains.Annotations;
using Microsoft.AspNetCore.SignalR;

namespace Proxy.Hubs
{
    class AssetHubState
    {
        public AsyncDuplexStreamingCall<CommandEnvelope, PositionBatch> GrpcConnection;
    }

    [PublicAPI]
    public class AssetHub : Hub
    {
        public string ConnectionId => $"connection{Context.ConnectionId}";

        private AssetHubState State {
            get {
                if (!Context.Items.ContainsKey("state")) Context.Items.Add("state", new AssetHubState());

                return Context.Items["state"] as AssetHubState;
            }
        }

        public async IAsyncEnumerable<PositionDTO[]> Connect()
        {
            Console.WriteLine("Connect user session " + ConnectionId);

            var channel =
                new Channel("127.0.0.1", 4040, ChannelCredentials.Insecure); //GrpcChannel.ForAddress(new Uri("https://localhost:4040"));
            
            var grpcClient = new Greeter.GreeterClient(channel);
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

        private static PositionDTO MapPositionDto(Position position) =>
            new()
            {
                Latitude = position.Latitude,
                Longitude = position.Longitude,
                Timestamp = position.Timestamp,
                // Course = position.Course,
                // Speed = position.Speed
            };
    }
}