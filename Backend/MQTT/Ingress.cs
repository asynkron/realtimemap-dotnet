using System;
using System.Collections.Concurrent;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Diagnostics;
using Newtonsoft.Json;
using Proto;
using Proto.Cluster;

namespace Backend.MQTT
{
    public class VehicleState
    {
        public bool DoorsOpen { get; set; }
    }

    public static class MqttIngress
    {
        public static async Task Start(Cluster cluster)
        {
            var state = new ConcurrentDictionary<string, VehicleState>();

            var mqttClient = CreateMqttClient();

            var mqttClientOptions = new MqttClientOptionsBuilder()
                .WithClientId("Client1")
                .WithTls(new MqttClientOptionsBuilderTlsParameters
                {
                    UseTls = true,
                    SslProtocol = SslProtocols.Tls12
                })
                .WithCleanSession()
                .WithTcpServer("mqtt.hsl.fi", 8883)
                .Build();

            mqttClient.UseApplicationMessageReceivedHandler(ProcessIncomingMessage(state, cluster));
            mqttClient.UseConnectedHandler(async args =>
            {
                Console.WriteLine("### CONNECTED WITH MQTT SERVER ###");

                await mqttClient.SubscribeAsync("/hfp/v2/journey/#");

                Console.WriteLine("### SUBSCRIBED ###");
            });
            mqttClient.UseDisconnectedHandler(async args =>
            {
                Console.WriteLine("### DISCONNECTED FROM MQTT SERVER ###");
                if (args.Exception is not null) Console.WriteLine(args.Exception);

                try
                {
                    await mqttClient.ConnectAsync(mqttClientOptions);
                }
                catch
                {
                    Console.WriteLine("### RECONNECTING FAILED ###");
                }
            });

            await mqttClient.ConnectAsync(mqttClientOptions);
        }

        private static IMqttClient CreateMqttClient()
        {
            var logger = new MqttNetLogger();
            logger.LogMessagePublished += (sender, e) =>
            {
                if (e.LogMessage.Level >= MqttNetLogLevel.Warning) Console.WriteLine(e.LogMessage.Message);

                if (e.LogMessage.Level == MqttNetLogLevel.Error) Console.WriteLine(e.LogMessage.Exception);
            };

            var factory = new MqttFactory(logger);
            var mqttClient = factory.CreateMqttClient();
            return mqttClient;
        }

        private static Func<MqttApplicationMessageReceivedEventArgs, Task> ProcessIncomingMessage(
            ConcurrentDictionary<string, VehicleState> state, Cluster cluster)
        {
            return async e =>
            {
                try
                {
                    //0/1       /2        /3             /4              /5           /6               /7            /8               /9         /10            /11        /12          /13         /14             /15       /16
                    // /<prefix>/<version>/<journey_type>/<temporal_type>/<event_type>/<transport_mode>/<operator_id>/<vehicle_number>/<route_id>/<direction_id>/<headsign>/<start_time>/<next_stop>/<geohash_level>/<geohash>/<sid>/#

                    var payload = e.ApplicationMessage.Payload;
                    var ascii = Encoding.ASCII.GetString(payload);
                    var typed = JsonConvert.DeserializeObject<Root>(ascii);
                    var parts = e.ApplicationMessage.Topic.Split("/");
                    var operatorId = parts[7];
                    var vehicleNumber = parts[8];

                    var vehicleId = operatorId + "." + vehicleNumber;

                    //state should not be handled here, it should be in the respective actor
                    var vs = state.GetOrAdd(vehicleId, x => new VehicleState());

                    Payload pl = null;

                    //the payload from MQTT here is super weird.
                    //there is a property on the root object which dictates what type of event this is
                    //e.g DOC = doors closed, DOO = doors opened. VP = vehicle position
                    if (typed.DoorsOpen != null)
                    {
                        pl = typed.DoorsOpen;
                        vs.DoorsOpen = true;
                    }

                    if (typed.DoorsClosed != null)
                    {
                        pl = typed.DoorsClosed;
                        vs.DoorsOpen = false;
                    }

                    if (typed.VehiclePosition != null) pl = typed.VehiclePosition;

                    if (pl != null && pl.HasValidPosition)
                    {
                        var p = new Position
                        {
                            OrgId = operatorId,
                            Longitude = pl.Long.Value,
                            Latitude = pl.Lat.Value,
                            VehicleId = vehicleId,
                            Heading = (int) pl.Hdg.Value,
                            DoorsOpen = vs.DoorsOpen
                        };

                        await cluster
                            .GetVehicleActor(p.VehicleId).OnPosition(p, CancellationTokens.FromSeconds(1));
                    }
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                    throw;
                }
            };
        }
    }
}