using Backend.Models;
using Proto.Cluster.PubSub;
using Proto.OpenTelemetry;

namespace Backend.Actors;

public class VehicleActor : VehicleActorBase
{
    private readonly MapGrid _mapGrid;
    private readonly VehiclePositionHistory _positionsHistory;
    private readonly IRootContext _senderContext;

    private long _lastPositionUpdateTimestamp;

    public VehicleActor(IContext context, MapGrid mapGrid) : base(context)
    {
        _mapGrid = mapGrid;
        _positionsHistory = new VehiclePositionHistory();
        _senderContext = Context.System.Root;
    }

    public override async Task OnPosition(Position position)
    {
        _positionsHistory.Add(position);

        if (position.Timestamp > _lastPositionUpdateTimestamp)
        {
            _ = Cluster
                .GetOrganizationActor(position.OrgId)
                .OnPosition(position, _senderContext, CancellationTokens.FromSeconds(1));

            // broadcast to any viewport that watches this area on the map
            var topic = _mapGrid.TopicFromPosition(position);
            if (topic != null)
            {
                var producer = SharedProducers.GetProducer(Cluster, topic);
                await producer.ProduceAsync(position);
            }

            _lastPositionUpdateTimestamp = position.Timestamp;
        }
    }

    public override Task<PositionBatch> GetPositionsHistory(GetPositionsHistoryRequest request)
    {
        var result = new PositionBatch();
        result.Positions.AddRange(_positionsHistory.Positions);

        return Task.FromResult(result);
    }
}