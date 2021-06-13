using System;
using System.Collections.Generic;
using Grpc.Core;
using JetBrains.Annotations;
using Microsoft.AspNetCore.SignalR;

namespace Proxy.Hubs
{
    class AssetHubState
    {
        public AsyncDuplexStreamingCall<Envelope, AssetStateEvents> GrpcConnection;
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

        public async IAsyncEnumerable<AssetStateEventDTO[]> Connect()
        {
            Console.WriteLine("Connect user session " + ConnectionId);

            var channel =
                new Channel("127.0.0.1", 4040, ChannelCredentials.Insecure); //GrpcChannel.ForAddress(new Uri("https://localhost:4040"));

            var grpcClient = new SensorProcessor.SensorProcessorClient(channel);
            var conn = grpcClient.Connect();
            State.GrpcConnection = conn;

            Console.WriteLine("Got response...");

            Envelope envelope = new()
            {
                CreateUserSession = new CreateUserSession()
                {
                    ConnectionId = ConnectionId
                }
            };

            await conn.RequestStream.WriteAsync(envelope);

            Console.WriteLine($"Connected {envelope}");

            var responseStream = State.GrpcConnection.ResponseStream;

            while (await responseStream.MoveNext())
            {
                var assetStateEvents = responseStream.Current.AssetStates.ToArray();

                if (assetStateEvents.Any())
                {
                    yield return assetStateEvents
                        .Select(MapAssetStateEventDto)
                        .ToArray();
                }
            }
        }

        private static AssetStateEventDTO MapAssetStateEventDto(AssetState e)
        {
            var position = e.State["position"].Unpack<Position>();
            return new()
            {
                AssetId = e.AssetId, AssetType = (int) e.AssetType, Position = new PositionStateDTO
                {
                    Latitude = position.Latitude,
                    Longitude = position.Longitude,
                    //Timestamp = position..Timestamp,
                    Course = position.Course,
                    Speed = position.Speed
                },
            };
        }
    }
}