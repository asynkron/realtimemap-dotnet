namespace MapBackend.Hubs
{
    public class AssetStateEventDTO
    {
        public string AssetId { get; set; }
        public int AssetType { get; set; }

        public PositionStateDTO Position { get; set; }
    }

    public class PositionStateDTO
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public ulong Timestamp { get; set; }
        public int? Course { get; set; }
        public float? Speed { get; set; }
    }
}