namespace Proxy.DTO
{
    public class NotificationDto : IHubMessageData
    {
        public string Message { get; set; }
    }
}