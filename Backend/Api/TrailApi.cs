using System.Net;
using Backend.DTO;
using Proto.OpenTelemetry;

namespace Backend.Api;

public static class TrailApi
{
    public static void MapTrailApi(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/trail/{vehicleId}", async (string vehicleId, Cluster cluster) =>
        {
            var positionsHistory = await cluster
                .GetVehicleActor(vehicleId)
                .GetPositionsHistory(
                    new GetPositionsHistoryRequest(), 
                    CancellationToken.None);

            if (positionsHistory == null) return Results.StatusCode((int)HttpStatusCode.ServiceUnavailable); // timeout

            var positions = positionsHistory.Positions
                .Select(PositionDto.MapFrom)
                .ToArray();

            var result = new PositionsDto(positions);

            
            return Results.Ok(result);
        });
    }
}