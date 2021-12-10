using System.Diagnostics;
using Backend.Actors;
using Backend.DTO;
using Backend.Infrastructure.Metrics;
using Backend.Infrastructure.Tracing;
using Microsoft.AspNetCore.SignalR;
using OpenTelemetry.Trace;
using Proto.OpenTelemetry;

namespace Backend.Hubs;

public class EventsHub : Hub
{
    private readonly Cluster _cluster;
    private readonly IHubContext<EventsHub> _eventsHubContext;
    private readonly ILogger<EventsHub> _logger;
    private readonly IRootContext _senderContext;

    public EventsHub(Cluster cluster, IHubContext<EventsHub> eventsHubContext, ILogger<EventsHub> logger)
    {
        _cluster = cluster;
        _senderContext = cluster.System.Root.WithTracing();
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
        RealtimeMapMetrics.SignalRConnections.ChangeBy(1);
        
        var connectionId = Context.ConnectionId;
        UserActorPid = _cluster.System.Root.Spawn(
            Props.FromProducer(() => new UserActor(
                    batch => SendPositionBatch(connectionId, batch),
                    notification => SendNotification(connectionId, notification)
                ))
                .WithTracing()
        );

        return Task.CompletedTask;
    }

    public Task SetViewport(double swLng, double swLat, double neLng, double neLat)
    {
        using var activity = SignalRActivitySource.ActivitySource.StartActivity(
            "hub.request " + nameof(SetViewport),
            ActivityKind.Server
        );
        activity?.SetTag("viewport", $"({swLat}, {swLng}),({neLat}, {neLng})");
        activity?.SetTag("connection.id", Context.ConnectionId);

        _logger.LogInformation("Client {ClientId} setting viewport to ({SWLat}, {SWLng}),({NELat}, {NELng})",
            Context.ConnectionId, swLat, swLng, neLat, neLng);

        _senderContext.Send(UserActorPid, new UpdateViewport
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
    {
        using var activity = SignalRActivitySource.ActivitySource.StartActivity(
            "hub.send " + nameof(PositionBatch),
            ActivityKind.Client);
        activity?.SetTag("connection.id", connectionId);

        try
        {
            await _eventsHubContext.Clients.Client(connectionId).SendAsync("positions",
                new PositionsDto
                {
                    Positions = batch.Positions
                        .Select(PositionDto.MapFrom)
                        .ToArray()
                });
        }
        catch (Exception e)
        {
            activity?.RecordException(e);
            activity?.SetStatus(ActivityStatusCode.Error);
            throw;
        }
    }

    private async Task SendNotification(string connectionId, Notification notification)
    {
        using var activity = SignalRActivitySource.ActivitySource.StartActivity(
            "hub.send " + nameof(Notification),
            ActivityKind.Client);
        activity?.SetTag("connection.id", connectionId);

        try
        {
            await _eventsHubContext.Clients.Client(connectionId)
                .SendAsync("notification", NotificationDto.MapFrom(notification));
        }
        catch (Exception e)
        {
            activity?.RecordException(e);
            activity?.SetStatus(ActivityStatusCode.Error);
            throw;
        }
    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        _logger.LogDebug("Client {ClientId} disconnected", Context.ConnectionId);
        RealtimeMapMetrics.SignalRConnections.ChangeBy(-1);

        await _cluster.System.Root.StopAsync(UserActorPid);
    }
}