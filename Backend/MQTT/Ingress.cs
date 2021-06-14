using System.Collections.Concurrent;
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
    public class VehicleState
    {
        public bool DoorsOpen { get; set; }
    }
    public static class MqttIngress
    {
        public static async Task<IAsyncEnumerable<Position>> Start()
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

            await mqttClient.ConnectAsync(options);
            await mqttClient.SubscribeAsync("/hfp/v2/journey/#");
            var state = new ConcurrentDictionary<string, VehicleState>();
            mqttClient.UseApplicationMessageReceivedHandler(async e =>
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

                var vs = state.GetOrAdd(vehicleId, x => new VehicleState());

                Payload pl = null;

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

                if (typed.VehiclePosition != null)
                {
                    pl = typed.VehiclePosition;
                }

                if (pl != null)
                {
                    var p = new Position()
                    {
                        Longitude = pl.Long,
                        Latitude = pl.Lat,
                        VehicleId = vehicleId,
                        Heading = (int) pl.Hdg,
                        DoorsOpen = vs.DoorsOpen,
                    };

                    await channel.Writer.WriteAsync(p);
                }
                //Console.WriteLine($"{vehicleId}: {typed.Vp.Lat}, {typed.Vp.Long}, {typed.Vp.Hdg}");
            });

            return channel.Reader.ReadAllAsync();
        }
    }
}