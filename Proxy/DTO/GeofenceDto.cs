using System.Collections.Generic;

namespace Proxy.DTO
{
    public class GeofenceDto
    {
        public GeofenceDto(string name, HashSet<string> vehiclesInZone)
        {
            Name = name;
            VehiclesInZone = vehiclesInZone;
        }

        public string Name { get; }
        public HashSet<string> VehiclesInZone { get; }
    }
}