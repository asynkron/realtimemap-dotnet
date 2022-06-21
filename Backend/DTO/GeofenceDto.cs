namespace Backend.DTO;

public record GeofenceDto(
    string Name,
    double Latitude,
    double Longitude,
    double RadiusInMeters,
    string[] VehiclesInZone
);