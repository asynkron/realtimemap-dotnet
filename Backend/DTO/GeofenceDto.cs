namespace Backend.DTO
{
    public class GeofenceDto
    {
        public string Name { get; set; }
        public double Longitude { get; set; }
        public double Latitude { get; set; }
        public double RadiusInMeters { get; set; }
        public string[] VehiclesInZone { get; set; }
    }
}