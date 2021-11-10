using System.Text;
using MQTTnet;
using Newtonsoft.Json;

namespace Backend.MQTT;

public record HrtPositionUpdate(string OperatorId, string VehicleNumber, Payload VehiclePosition)
{
    public static HrtPositionUpdate ParseFromMqttMessage(MqttApplicationMessage mqttApplicationMessage)
    {
        var topic = HrtPositionUpdateTopic.FromMqttMessage(mqttApplicationMessage);

        var payload = JsonConvert.DeserializeObject<Root>(
            Encoding.UTF8.GetString(mqttApplicationMessage.Payload)
        );
            
        var vehiclePosition = payload?.VehiclePosition;

        return new HrtPositionUpdate(
            OperatorId: topic.OperatorId,
            VehicleNumber: topic.VehicleNumber,
            VehiclePosition: vehiclePosition);
    }

    public bool HasValidPosition => VehiclePosition != null && VehiclePosition.HasValidPosition;
}