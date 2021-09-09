using System;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Diagnostics;
using Newtonsoft.Json;
using Proto;
using Proto.Cluster;

namespace Backend.MQTT
{
    public class MqttIngress : IHostedService
    {
        private readonly Cluster _cluster;

        public MqttIngress(Cluster cluster)
        {
            _cluster = cluster;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await Start(_cluster);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private async Task Start(Cluster cluster)
        {
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

            mqttClient.UseApplicationMessageReceivedHandler(ProcessIncomingMessage(cluster));
            mqttClient.UseConnectedHandler(async args =>
            {
                Console.WriteLine("### CONNECTED WITH MQTT SERVER ###");

                await mqttClient.SubscribeAsync("/hfp/v2/journey/ongoing/vp/bus/#");

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

        private static Func<MqttApplicationMessageReceivedEventArgs, Task> ProcessIncomingMessage(Cluster cluster)
        {
            return async e =>
            {
                try
                {
                    //0/1       /2        /3             /4              /5           /6               /7            /8               /9         /10            /11        /12          /13         /14             /15       /16
                    // /<prefix>/<version>/<journey_type>/<temporal_type>/<event_type>/<transport_mode>/<operator_id>/<vehicle_number>/<route_id>/<direction_id>/<headsign>/<start_time>/<next_stop>/<geohash_level>/<geohash>/<sid>/#

                    var payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                    var typed = JsonConvert.DeserializeObject<Root>(payload);
                    var parts = e.ApplicationMessage.Topic.Split("/");
                    var operatorId = parts[7];
                    var vehicleNumber = parts[8];

                    var vehicleId = operatorId + "." + vehicleNumber;

                    Payload pl = null;
                    
                    if (typed?.VehiclePosition != null) pl = typed.VehiclePosition;

                    if (pl != null && pl.HasValidPosition)
                    {
                        var p = new Position
                        {
                            OrgId = operatorId,
                            Longitude = pl.Long.GetValueOrDefault(),
                            Latitude = pl.Lat.GetValueOrDefault(),
                            VehicleId = vehicleId,
                            Heading = (int) pl.Hdg.GetValueOrDefault(),
                            DoorsOpen = pl.Drst == 1,
                            Timestamp = pl.Tst.GetValueOrDefault().Ticks,
                            Speed = pl.Spd.GetValueOrDefault()
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