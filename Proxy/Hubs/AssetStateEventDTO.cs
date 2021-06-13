namespace Proxy.Hubs
{
    public class PositionDTO
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public ulong Timestamp { get; set; }
        public int? Course { get; set; }
        public float? Speed { get; set; }
    }
}