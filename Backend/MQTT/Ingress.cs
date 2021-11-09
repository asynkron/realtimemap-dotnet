using Proto;
using Proto.Cluster;

namespace Backend.MQTT;

public class MqttIngress : IHostedService
{
    private readonly IConfiguration _configuration;
    private readonly Cluster _cluster;

    private HrtPositionsSubscription _hrtPositionsSubscription;

    public MqttIngress(IConfiguration configuration, Cluster cluster)
    {
        _configuration = configuration;
        _cluster = cluster;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _hrtPositionsSubscription = await HrtPositionsSubscription.Start(
            sharedSubscriptionGroupName: GetSharedSubscriptionGroupName(),
            onPositionUpdate: ProcessHrtPositionUpdate);
    }

    private string GetSharedSubscriptionGroupName()
    {
        var sharedSubscriptionGroupName = _configuration["RealtimeMap:SharedSubscriptionGroupName"];

        return string.IsNullOrEmpty(sharedSubscriptionGroupName)
            ? $"group-{Guid.NewGuid()}"
            : sharedSubscriptionGroupName;
    }

    private async Task ProcessHrtPositionUpdate(HrtPositionUpdate hrtPositionUpdate)
    {
        var vehicleId = $"{hrtPositionUpdate.OperatorId}.{hrtPositionUpdate.VehicleNumber}";

        var position = new Position
        {
            OrgId = hrtPositionUpdate.OperatorId,
            Longitude = hrtPositionUpdate.VehiclePosition.Long.GetValueOrDefault(),
            Latitude = hrtPositionUpdate.VehiclePosition.Lat.GetValueOrDefault(),
            VehicleId = vehicleId,
            Heading = (int)hrtPositionUpdate.VehiclePosition.Hdg.GetValueOrDefault(),
            DoorsOpen = hrtPositionUpdate.VehiclePosition.Drst == 1,
            Timestamp = hrtPositionUpdate.VehiclePosition.Tst.GetValueOrDefault().Ticks,
            Speed = hrtPositionUpdate.VehiclePosition.Spd.GetValueOrDefault()
        };

        await _cluster
            .GetVehicleActor(vehicleId)
            .OnPosition(position, CancellationTokens.FromSeconds(1));
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _hrtPositionsSubscription?.Dispose();

        return Task.CompletedTask;
    }
}