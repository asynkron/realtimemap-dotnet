using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Backend;
using Backend.Models;
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
            var results = Organizations.All
                .Where(organization => organization.Geofences.Any())
                .Select(organization => new OrganizationDto
                {
                    Id = organization.Id,
                    Name = organization.Name
                })
                .OrderBy(organization => organization.Name)
                .ToList();

            return Ok(results);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<OrganizationDetailsDto>> Get(string id)
        {
            if (!Organizations.ById.TryGetValue(id, out var organization))
            {
                return NotFound();
            }
            
            var geofences = await _client.GetOrganizationGeofencesAsync(new GetGeofencesRequest
            {
                OrgId = id
            });

            var results = new OrganizationDetailsDto
            {
                Id = organization.Id,
                Name = organization.Name,
                Geofences = geofences.Geofences
                    .Select(geofence => new GeofenceDto
                    {
                        Name = geofence.Name,
                        VehiclesInZone = geofence.VehiclesInZone
                            .OrderBy(zone => zone)
                            .ToArray()
                    })
                    .OrderBy(geofence => geofence.Name)
                    .ToList()
            };

            return Ok(results);
        }
    }
}