using System.Threading.Tasks;
using Proto;

namespace Backend.Actors
{
    public class ViewportActor : ViewportActorBase
    {
        public ViewportActor(IContext context) : base(context)
        {
        }

        public override Task<Ack> UpdateViewport(UpdateViewport request)
        {
            throw new System.NotImplementedException();
        }
    }
}