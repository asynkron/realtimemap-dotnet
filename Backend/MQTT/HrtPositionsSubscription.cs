using System;
using System.Security.Authentication;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Diagnostics;

namespace Backend.MQTT
{
    public class HrtPositionsSubscription : IDisposable
    {
        public static async Task<HrtPositionsSubscription> Start(Func<HrtPositionUpdate, Task> onPositionUpdate)
        {
            var mqttClient = CreateMqttClient();

            var mqttClientOptions = new MqttClientOptionsBuilder()
                .WithClientId(null) // do not keep state on the broker
                .WithCleanSession()
                .WithTls(new MqttClientOptionsBuilderTlsParameters
                {
                    UseTls = true,
                    SslProtocol = SslProtocols.Tls12
                })
                .WithTcpServer("mqtt.hsl.fi", 8883)
                .Build();

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
            
            mqttClient.UseApplicationMessageReceivedHandler(async e =>
            {
                try
                {
                    var hrtPositionUpdate = HrtPositionUpdate.ParseFromMqttMessage(e.ApplicationMessage);

                    if (hrtPositionUpdate.HasValidPosition)
                    {
                        await onPositionUpdate(hrtPositionUpdate);
                    }
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                    throw;
                }
            });

            await mqttClient.ConnectAsync(mqttClientOptions);

            return new HrtPositionsSubscription(mqttClient);
        }
        
        private static IMqttClient CreateMqttClient()
        {
            var logger = CreateMqttNetLogger();
            var factory = new MqttFactory(logger);

            return factory.CreateMqttClient();
        }

        private static MqttNetLogger CreateMqttNetLogger()
        {
            var logger = new MqttNetLogger();

            logger.LogMessagePublished += (sender, e) =>
            {
                if (e.LogMessage.Level >= MqttNetLogLevel.Warning) Console.WriteLine(e.LogMessage.Message);

                if (e.LogMessage.Level == MqttNetLogLevel.Error) Console.WriteLine(e.LogMessage.Exception);
            };
            
            return logger;
        }

        private readonly IMqttClient _mqttClient;

        private HrtPositionsSubscription(IMqttClient mqttClient)
        {
            _mqttClient = mqttClient;
        }

        public void Dispose()
        {
            _mqttClient?.Dispose();
        }
    }
}