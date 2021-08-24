namespace Proxy.DTO
{
    public class HubMessageDto
    {
        public HubMessageDto(HubMessageType messageType, IHubMessageData data)
        {
            Payload = new HubPayload
            {
                MessageType = messageType
            };

            Data = data;
        }
        
        public HubPayload Payload { get; }
        
        public object Data { get; }
    }

    public interface IHubMessageData
    {
    }

    public class HubPayload
    {
        public HubMessageType MessageType { get; set; }
    }

    public enum HubMessageType
    {
        Position = 1,
        Notification = 2
    }
}