using System.Collections.Generic;

namespace Backend.DTO
{
    public class OrganizationDetailsDto : OrganizationDto
    {
        public IReadOnlyList<GeofenceDto> Geofences { get; set; }
    }
}