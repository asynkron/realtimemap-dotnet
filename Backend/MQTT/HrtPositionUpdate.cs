using System.Text;
using MQTTnet;
using Newtonsoft.Json;

namespace Backend.MQTT;

public record HrtPositionUpdate(string OperatorId, string VehicleNumber, Payload Payload)
{
    public static HrtPositionUpdate? ParseFromMqttMessage(MqttApplicationMessage mqttApplicationMessage)
    {
        var topic = HrtPositionUpdateTopic.FromMqttMessage(mqttApplicationMessage);

        var msg = JsonConvert.DeserializeObject<Root>(
            Encoding.UTF8.GetString(mqttApplicationMessage.Payload)
        );
            
        var payload = msg?.VehiclePosition ?? msg?.DoorsOpen ?? msg?.DoorsClosed;

        if (payload?.HasValidPosition == true)
        {
            return new HrtPositionUpdate(
                OperatorId: topic.OperatorId,
                VehicleNumber: topic.VehicleNumber,
                Payload: payload);
        }

        return null;
    }
}