using System.Threading.Tasks;
using Proto;

namespace Backend.Actors
{
    public class VehicleActor : VehicleActorBase
    {
        private readonly VehiclePositionHistory _positionsHistory;
        
        public VehicleActor(IContext context) : base(context)
        {
            _positionsHistory = new VehiclePositionHistory();
        }

        public override Task OnPosition(Position position)
        {
            _positionsHistory.Add(position);
            
            //broadcast event on all cluster members eventstream
            _ = Cluster.GetOrganizationActor(position.OrgId).OnPosition(position, CancellationTokens.FromSeconds(1));
            Cluster.MemberList.BroadcastEvent(position);

            return Task.CompletedTask;
        }

        public override Task<PositionBatch> GetPositionsHistory(GetPositionsHistoryRequest request)
        {
            var result = new PositionBatch();
            result.Positions.AddRange(_positionsHistory.Positions);

            return Task.FromResult(result);
        }
    }
}