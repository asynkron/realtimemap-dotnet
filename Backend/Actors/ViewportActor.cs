using System.Threading.Tasks;
using Proto;

namespace Backend.Actors
{
    public class ViewportActor : ViewportActorBase
    {
        private EventStreamSubscription<object> _sub;

        public ViewportActor(IContext context) : base(context)
        {
        }

        public override Task OnStarted()
        {
            _sub = System.EventStream.Subscribe<Position>(Context, Context.Self);
            return Task.CompletedTask;
        }

        public override Task OnStopping()
        {
            _sub.Unsubscribe();
            return Task.CompletedTask;
        }

        //Why is this needed here?
        //Because we are not getting the position as a grain request.
        //We are getting this as a raw event via the event-stream, so we have to manually map this to a handler
        public override Task OnReceive()
        {
            if (Context.Message is Position pos)
            {
                return OnPosition(pos);
            }
            return base.OnReceive();
        }

        private Task OnPosition(Position pos)
        {
            return Task.CompletedTask;
        }


        public override async Task UpdateViewport(UpdateViewport request)
        {
     
        }
    }
}