using System.Net;
using Backend.DTO;
using Backend.Models;
using Proto.OpenTelemetry;

namespace Backend.Api;

public static class OrganizationApi
{
    public static void MapOrganizationApi(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/organization", () =>
        {
            var results = Organizations.All
                .Where(organization => organization.Geofences.Any())
                .Select(organization => new OrganizationDto(organization.Id, organization.Name))
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
                    new GetGeofencesRequest {OrgId = id},
                    CancellationToken.None
                );

                if (geofences == null) return Results.StatusCode((int) HttpStatusCode.ServiceUnavailable); // timeout

                var results = new OrganizationDetailsDto(
                    organization.Id,
                    organization.Name,
                    geofences.Geofences
                        .Select(geofence => new GeofenceDto(
                            geofence.Name,
                            geofence.Latitude,
                            geofence.Longitude,
                            geofence.RadiusInMeters,
                            geofence.VehiclesInZone
                                .OrderBy(zone => zone)
                                .ToArray()
                        ))
                        .OrderBy(geofence => geofence.Name)
                        .ToList()
                );

                return Results.Ok(results);
            }
        );
    }
}