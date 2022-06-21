namespace Backend.DTO;

public record NotificationDto(string VehicleId, string OrgId, string OrgName, string ZoneName, string Event)
{
    public static NotificationDto MapFrom(Notification notification)
        => new(
            notification.VehicleId,
            notification.OrgId,
            notification.OrgName,
            notification.ZoneName,
            notification.Event.ToString()
        );
}