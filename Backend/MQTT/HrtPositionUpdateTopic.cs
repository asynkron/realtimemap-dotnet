using MQTTnet;

namespace Backend.MQTT
{
    public class HrtPositionUpdateTopic
    {
        public static HrtPositionUpdateTopic FromMqttMessage(MqttApplicationMessage mqttApplicationMessage)
        {
            var parts = mqttApplicationMessage.Topic.Split("/");

            return new HrtPositionUpdateTopic(parts);
        }
        
        private readonly string[] _parts;

        private HrtPositionUpdateTopic(string[] parts) => _parts = parts;

        // indexes documented at:
        // https://digitransit.fi/en/developers/apis/4-realtime-api/vehicle-positions/#the-topic
        
        public string OperatorId => _parts[7];
        public string VehicleNumber => _parts[8];
    }
}