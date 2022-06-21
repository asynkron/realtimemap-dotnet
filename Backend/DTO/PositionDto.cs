namespace Backend.DTO;

public record PositionDto(
    string VehicleId,
    double Latitude,
    double Longitude,
    long Timestamp,
    int Heading,
    float? Speed,
    bool DoorsOpen
)
{
    public static PositionDto MapFrom(Position position)
    {
        return new(
            position.VehicleId,
            position.Latitude,
            position.Longitude,
            position.Timestamp,
            position.Heading,
            10, // TODO?
            position.DoorsOpen
        );
    }
}

public record PositionsDto(PositionDto[] Positions);