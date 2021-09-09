using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Backend.DTO;
using Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Proto.Cluster;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/organization")]
    public class OrganizationController : ControllerBase
    {
        private readonly Cluster _cluster;

        public OrganizationController(Cluster cluster)
        {
            _cluster = cluster;
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

            var organizationActorClient = _cluster.GetOrganizationActor(id);

            var geofences = await organizationActorClient.GetGeofences(
                new GetGeofencesRequest { OrgId = id },
                CancellationToken.None
            );

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