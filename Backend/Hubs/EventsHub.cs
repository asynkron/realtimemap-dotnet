using Backend.Actors;
using Backend.DTO;
using Microsoft.AspNetCore.SignalR;

namespace Backend.Hubs;

public class EventsHub : Hub
{
    private readonly Cluster _cluster;
    private readonly IHubContext<EventsHub> _eventsHubContext;
    private readonly ILogger<EventsHub> _logger;

    public EventsHub(Cluster cluster, IHubContext<EventsHub> eventsHubContext, ILogger<EventsHub> logger)
    {
        _cluster = cluster;
        _logger = logger;

        // since the Hub is scoped per request, we need the IHubContext to be able to
        // push messages from the User actor
        _eventsHubContext = eventsHubContext;
    }

    private PID UserActorPid
    {
        get => Context.Items["user-pid"] as PID;
        set => Context.Items["user-pid"] = value;
    }

    public override Task OnConnectedAsync()
    {
        _logger.LogInformation("Client {ClientId} connected", Context.ConnectionId);

        var connectionId = Context.ConnectionId;
        UserActorPid = _cluster.System.Root.Spawn(
            Props.FromProducer(() => new UserActor(
                batch => SendPositionBatch(connectionId, batch),
                notification => SendNotification(connectionId, notification)
            ))
        );

        return Task.CompletedTask;
    }

    public Task SetViewport(double swLng, double swLat, double neLng, double neLat)
    {
        _logger.LogInformation("Client {ClientId} setting viewport to ({SWLat}, {SWLng}),({NELat}, {NELng})",
            Context.ConnectionId, swLat, swLng, neLat, neLng);

        _cluster.System.Root.Send(UserActorPid, new UpdateViewport
        {
            Viewport = new Viewport
            {
                SouthWest = new GeoPoint(swLng, swLat),
                NorthEast = new GeoPoint(neLng, neLat)
            }
        });

        return Task.CompletedTask;
    }

    private async Task SendPositionBatch(string connectionId, PositionBatch batch)
        => await _eventsHubContext.Clients.Client(connectionId).SendAsync("positions",
            new PositionsDto
            {
                Positions = batch.Positions
                    .Select(PositionDto.MapFrom)
                    .ToArray()
            });

    private async Task SendNotification(string connectionId, Notification notification)
        => await _eventsHubContext.Clients.Client(connectionId)
            .SendAsync("notification", NotificationDto.MapFrom(notification));

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        _logger.LogDebug("Client {ClientId} disconnected", Context.ConnectionId);

        await _cluster.System.Root.StopAsync(UserActorPid);
    }
}