namespace Backend.DTO;

public record OrganizationDetailsDto(
    string Id,
    string Name,
    IReadOnlyList<GeofenceDto> Geofences
) : OrganizationDto(Id, Name);