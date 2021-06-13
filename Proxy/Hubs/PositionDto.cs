namespace Proxy.Hubs
{
    public class PositionDto
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public ulong Timestamp { get; set; }
        public int Heading { get; set; }
        public float? Speed { get; set; }
    }
}