using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Proto;
using Proto.Cluster;

namespace Backend.MQTT
{
    public class MqttIngress : IHostedService
    {
        private readonly Cluster _cluster;

        private HrtPositionsSubscription _hrtPositionsSubscription;

        public MqttIngress(Cluster cluster)
        {
            _cluster = cluster;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _hrtPositionsSubscription = await HrtPositionsSubscription.Start(
                onPositionUpdate: ProcessHrtPositionUpdate);
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
                Heading = (int) hrtPositionUpdate.VehiclePosition.Hdg.GetValueOrDefault(),
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
            _hrtPositionsSubscription.Dispose();
            
            return Task.CompletedTask;
        }
    }
}