using System.Threading.Tasks;
using Proto;

namespace Backend.Actors
{
    public class VehicleActor : VehicleActorBase
    {
        public VehicleActor(IContext context) : base(context)
        {
        }

        public override Task<Ack> OnPosition(Position position)
        {
            throw new System.NotImplementedException();
        }
    }
}