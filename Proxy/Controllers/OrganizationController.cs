using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Backend;
using Microsoft.AspNetCore.Mvc;
using Proxy.DTO;

namespace Proxy.Controllers
{
    [ApiController]
    [Route("api/organization")]
    public class OrganizationController : ControllerBase
    {
        private readonly MapBackend.MapBackendClient _client;

        public OrganizationController(MapBackend.MapBackendClient client)
        {
            _client = client;
        }

        [HttpGet]
        public ActionResult<IReadOnlyList<OrganizationDto>> Browse()
        {
            var result = OrganizationsMap.Data.Select(x => new OrganizationDto
            {
                Id = x.Key,
                Name = x.Value
            }).ToList();

            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<OrganizationDetailsDto>> Get(string id)
        {
            if (OrganizationsMap.Data.ContainsKey(id) is false) return NotFound();

            var organization = OrganizationsMap.Data.Single(x => x.Key == id);

            var organizationGeofences =
                await _client.GetOrganizationGeofencesAsync(new GetGeofencesRequest {OrgId = id});

            var organizationDetails = new OrganizationDetailsDto
            {
                Id = organization.Key,
                Name = organization.Value,
                Geofences = organizationGeofences.Geofences
                    .Select(x => new GeofenceDto(x.Name, x.VehiclesInZone.ToHashSet())).ToList()
            };

            return Ok(organizationDetails);
        }
    }
}