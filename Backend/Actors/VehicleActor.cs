using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Proto;

namespace Backend.Actors
{
    public class VehicleActor : VehicleActorBase
    {
        private Position? _currentPosition;
        private List<Position> _positionsHistory;
        
        public VehicleActor(IContext context) : base(context)
        {
            _positionsHistory = new List<Position>();
        }

        public override async Task OnPosition(Position position)
        {
            //if (position.Timestamp > _currentPosition?.Timestamp)
            //{
            //   we could do this to handle ordering if relevant
            //}

            _currentPosition = position;
            _positionsHistory.Add(position);
            
            //broadcast event on all cluster members eventstream
            _ = Cluster.GetOrganizationActor(position.OrgId).OnPosition(position, CancellationTokens.FromSeconds(1));
            Cluster.MemberList.BroadcastEvent(position);
        }

        public override Task<PositionBatch> GetPositionsHistory(GetPositionsHistoryRequest request)
        {
            var result = new PositionBatch();
            result.Positions.AddRange(_positionsHistory);

            return Task.FromResult(result);
        }
    }
}