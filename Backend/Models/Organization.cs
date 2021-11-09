namespace Backend.Models
{
    public class Organization
    {
        public string Id { get; }
        public string Name { get; }
        public IReadOnlyList<CircularGeofence> Geofences { get; }

        public Organization(string id, string name, params CircularGeofence[] geofences)
        {
            Id = id;
            Name = name;
            Geofences = geofences.ToArray();
        }
    }
}