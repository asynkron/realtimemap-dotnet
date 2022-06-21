using Backend.Models;
using Proto.OpenTelemetry;

namespace Backend.Actors;

public class OrganizationActor : OrganizationActorBase
{
    private readonly ILogger<OrganizationActor> _logger;
    private string? _organizationName;

    public OrganizationActor(IContext context, ILogger<OrganizationActor> logger) : base(context)
    {
        _logger = logger;
    }

    public override Task OnStarted()
    {
        var organizationId = Context.Get<ClusterIdentity>()!.Identity;
        var organization = GetOrganization(organizationId);
        _organizationName = organization.Name;

        _logger.LogInformation("Started actor for organization: {OrganizationId} - {OrganizationName}", organizationId,
            _organizationName);

        foreach (var geofence in organization.Geofences)
        {
            CreateGeofenceActor(geofence);
        }

        return Task.CompletedTask;
    }

    private static Organization GetOrganization(string organizationId)
    {
        return Organizations.ById.TryGetValue(organizationId, out var foundOrganization)
            ? foundOrganization
            : new Organization(organizationId, organizationId);
    }

    private void CreateGeofenceActor(CircularGeofence circularGeofence)
    {
        var geofenceProps = Props.FromProducer(() => new GeofenceActor(
                _organizationName!,
                circularGeofence,
                Cluster
            ))
            .WithTracing();

        Context.Spawn(geofenceProps);
    }

    public override Task OnPosition(Position request)
    {
        foreach (var child in Context.Children) Context.Send(child, request);

        return Task.CompletedTask;
    }

    public override async Task<GetGeofencesResponse> GetGeofences(GetGeofencesRequest request)
    {
        var result = new GetGeofencesResponse();

        var tasks = Context.Children
            .Select(child => Context.RequestAsync<GeofenceDetails>(child, request))
            .ToList();

        foreach (var task in tasks) result.Geofences.Add(await task);

        return result;
    }
}