using Backend.Infrastructure.Metrics;
using Proto.OpenTelemetry;

namespace Backend.MQTT;

public class MqttIngress : IHostedService
{
    private readonly IConfiguration _configuration;
    private readonly Cluster _cluster;
    private readonly IRootContext _senderContext;
    private readonly ILoggerFactory _loggerFactory;
    private HrtPositionsSubscription? _hrtPositionsSubscription;

    public MqttIngress(IConfiguration configuration, Cluster cluster, ILoggerFactory loggerFactory)
    {
        _configuration = configuration;
        _cluster = cluster;
        _senderContext = _cluster.System.Root;
        _loggerFactory = loggerFactory;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _hrtPositionsSubscription = await HrtPositionsSubscription.Start(
            GetSharedSubscriptionGroupName(),
            ProcessHrtPositionUpdate,
            _loggerFactory,
            cancellationToken);
    }

    private string GetSharedSubscriptionGroupName()
    {
        var sharedSubscriptionGroupName = _configuration["RealtimeMap:SharedSubscriptionGroupName"];

        return string.IsNullOrEmpty(sharedSubscriptionGroupName)
            ? $"group-{Guid.NewGuid()}"
            : sharedSubscriptionGroupName;
    }

    private Task ProcessHrtPositionUpdate(HrtPositionUpdate hrtPositionUpdate)
    {
        var vehicleId = $"{hrtPositionUpdate.OperatorId}.{hrtPositionUpdate.VehicleNumber}";

        var position = new Position
        {
            OrgId = hrtPositionUpdate.OperatorId,
            Longitude = hrtPositionUpdate.Payload.Long.GetValueOrDefault(),
            Latitude = hrtPositionUpdate.Payload.Lat.GetValueOrDefault(),
            VehicleId = vehicleId,
            Heading = (int)hrtPositionUpdate.Payload.Hdg.GetValueOrDefault(),
            DoorsOpen = hrtPositionUpdate.Payload.Drst == 1,
            Timestamp = hrtPositionUpdate.Payload.Tst.GetValueOrDefault().ToUnixTimeMilliseconds(),
            Speed = hrtPositionUpdate.Payload.Spd.GetValueOrDefault()
        };

        _ = _cluster
            .GetVehicleActor(vehicleId)
            .OnPosition(position, _senderContext, CancellationTokens.FromSeconds(1));

        RealtimeMapMetrics.MqttMessageLeadTime.Record(
            (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - position.Timestamp) / 1000.0);
        
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _hrtPositionsSubscription?.Dispose();
        return Task.CompletedTask;
    }
}