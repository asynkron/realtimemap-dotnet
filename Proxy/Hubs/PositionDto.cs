namespace Proxy.Hubs
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
    }

    public class PositionsDto
    {
        public PositionDto[] Positions { get; set; }
    }
}