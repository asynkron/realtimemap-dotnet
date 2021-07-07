using System.Threading.Tasks;
using Proto;
using Proto.Cluster;

namespace Backend.Actors
{
    public class VehicleActor : VehicleActorBase
    {
        public VehicleActor(IContext context) : base(context)
        {
        }

        public override async Task<Ack> OnPosition(Position position)
        {
            //broadcast event on all cluster members eventstream
            Cluster.MemberList.BroadcastEvent(position);

            return new Ack();
        }
    }
}