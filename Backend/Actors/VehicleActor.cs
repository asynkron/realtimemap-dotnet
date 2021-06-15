using System.Threading.Tasks;
using Proto;

namespace Backend.Actors
{
    public class VehicleActor : VehicleActorBase
    {
        public VehicleActor(IContext context) : base(context)
        {
        }

        public override Task<PositionBatch> OnPosition(CommandEnvelope request)
        {
            throw new System.NotImplementedException();
        }
    }
}