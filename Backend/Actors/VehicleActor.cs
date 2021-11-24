using Backend.ProtoActorTracing;

namespace Backend.Actors;

public class VehicleActor : VehicleActorBase
{
    private readonly VehiclePositionHistory _positionsHistory;
    private readonly IRootContext _senderContext;

    private long _lastPositionUpdateTimestamp;

    public VehicleActor(IContext context) : base(context)
    {
        _positionsHistory = new VehiclePositionHistory();
        _senderContext = Context.System.Root.WithOpenTelemetry();
    }

    public override Task OnPosition(Position position)
    {
        _positionsHistory.Add(position);

        if (position.Timestamp > _lastPositionUpdateTimestamp)
        {
            _ = Cluster
                .GetOrganizationActor(position.OrgId)
                .OnPosition(position, _senderContext, CancellationTokens.FromSeconds(1));

            // broadcast event on all cluster members eventstream
            Cluster.MemberList.BroadcastEvent(position);

            _lastPositionUpdateTimestamp = position.Timestamp;
        }

        return Task.CompletedTask;
    }

    public override Task<PositionBatch> GetPositionsHistory(GetPositionsHistoryRequest request)
    {
        var result = new PositionBatch();
        result.Positions.AddRange(_positionsHistory.Positions);

        return Task.FromResult(result);
    }
}