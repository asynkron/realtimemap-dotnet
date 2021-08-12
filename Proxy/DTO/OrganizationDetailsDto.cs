using System.Collections.Generic;

namespace Proxy.DTO
{
    public class OrganizationDetailsDto : OrganizationDto
    {
        public IReadOnlyList<GeofenceDto> Geofences { get; set; }
    }
}