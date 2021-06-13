using System.Collections.Generic;
using System.Security.Authentication;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using Newtonsoft.Json;

namespace Backend.MQTT
{
    class MqttIngress
    {
        static async Task<IAsyncEnumerable<Position>> Start()
        {
            var factory = new MqttFactory();
            var mqttClient = factory.CreateMqttClient();
            var channel = Channel.CreateUnbounded<Position>();

            var options = new MqttClientOptionsBuilder()
                .WithClientId("Client1")
                .WithTls(new MqttClientOptionsBuilderTlsParameters()
                {
                    UseTls = true,
                    SslProtocol = SslProtocols.Tls12,
                })
                .WithCleanSession()
                .WithTcpServer("mqtt.hsl.fi", 8883)
                .Build();

            var x = await mqttClient.ConnectAsync(options);
            var sub = await mqttClient.SubscribeAsync("/hfp/v2/journey/#");
            mqttClient.UseApplicationMessageReceivedHandler(async e =>
            {
                //0/1       /2        /3             /4              /5           /6               /7            /8               /9         /10            /11        /12          /13         /14             /15       /16
                // /<prefix>/<version>/<journey_type>/<temporal_type>/<event_type>/<transport_mode>/<operator_id>/<vehicle_number>/<route_id>/<direction_id>/<headsign>/<start_time>/<next_stop>/<geohash_level>/<geohash>/<sid>/#
                
                var payload = e.ApplicationMessage.Payload;
                var ascii = Encoding.ASCII.GetString(payload);
                var typed = JsonConvert.DeserializeObject<Root>(ascii);
                var parts = e.ApplicationMessage.Topic.Split("/");
                var vehicleId = parts[8];

                var p = new Position()
                {
                    Longitude = typed.Vp.Long,
                    Latitude = typed.Vp.Lat,
                    VehicleId = vehicleId,
                    Heading = (int)typed.Vp.Hdg,
                };
                
                await channel.Writer.WriteAsync(p);
                //Console.WriteLine($"{vehicleId}: {typed.Vp.Lat}, {typed.Vp.Long}, {typed.Vp.Hdg}");
            });

            return channel.Reader.ReadAllAsync();
        }
    }
}