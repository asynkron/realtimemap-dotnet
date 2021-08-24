using Backend;

namespace Proxy.DTO
{
    public class PositionDto
    {
        public string VehicleId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public ulong Timestamp { get; set; }
        public int Heading { get; set; }
        public float? Speed { get; set; }

        public bool DoorsOpen { get; set; }

        public static PositionDto MapFrom(Position position)
        {
            return new()
            {
                Latitude = position.Latitude,
                Longitude = position.Longitude,
                Timestamp = position.Timestamp,
                Heading = position.Heading,
                VehicleId = position.VehicleId,
                Speed = 10,
                DoorsOpen = position.DoorsOpen
            };
        }
    }

    public class PositionsDto : IHubMessageData
    {
        public PositionDto[] Positions { get; set; }
    }
}