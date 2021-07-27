using System;
using System.Threading;
using System.Threading.Tasks;
using Proto;
using Proto.Cluster;

namespace Backend.Actors
{
    public class VehicleActor : VehicleActorBase
    {
        private Position? _currentPosition;
        public VehicleActor(IContext context) : base(context)
        {
        }

        public override async Task OnPosition(Position position)
        {
            //if (position.Timestamp > _currentPosition?.Timestamp)
            //{
            //   we could do this to handle ordering if relevant
            //}

            _currentPosition = position;
            
            //broadcast event on all cluster members eventstream
            _ = Cluster.GetOrganizationActor(position.OrgId).OnPosition(position, CancellationTokens.FromSeconds(1));
            Cluster.MemberList.BroadcastEvent(position);
        }
    }
}