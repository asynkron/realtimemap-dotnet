using Backend.DTO;
using Backend.Models;

namespace Backend.Api;

public static class OrganizationApi
{
    public static void MapOrganizationApi(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/organization", () =>
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

            return Results.Ok(results);
        });

        app.MapGet("/api/organization/{id}", async (string id, Cluster cluster) =>
            {
                if (!Organizations.ById.TryGetValue(id, out var organization))
                    return Results.NotFound();

                var organizationActorClient = cluster.GetOrganizationActor(id);

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
                            RadiusInMeters = geofence.RadiusInMeters,
                            Longitude = geofence.Longitude,
                            Latitude = geofence.Latitude,
                            VehiclesInZone = geofence.VehiclesInZone
                                .OrderBy(zone => zone)
                                .ToArray()
                        })
                        .OrderBy(geofence => geofence.Name)
                        .ToList()
                };

                return Results.Ok(results);
            }
        );
    }
}