namespace Backend.DTO;

public class NotificationDto
{
    public string VehicleId { get; set; }
    
    public string OrgId { get; set; }
    
    public string OrgName { get; set; }

    public string ZoneName { get; set; }
    
    public string Event { get; set; }

    public static NotificationDto MapFrom(Notification notification)
        => new()
        {
            VehicleId = notification.VehicleId,
            OrgId = notification.OrgId,
            OrgName = notification.OrgName,
            ZoneName = notification.ZoneName,
            Event = notification.Event.ToString()
        };
}