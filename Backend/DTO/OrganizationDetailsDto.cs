namespace Backend.DTO;

public class OrganizationDetailsDto : OrganizationDto
{
    public IReadOnlyList<GeofenceDto> Geofences { get; set; }
}