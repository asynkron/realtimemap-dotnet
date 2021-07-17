using System;
using System.Threading.Tasks;
using Proto;

namespace Backend.Actors
{
    public class OrganizationActor : OrganizationActorBase
    {
        public OrganizationActor(IContext context) : base(context)
        {
        }

        public override Task OnStarted()
        {
            Console.WriteLine("Org " + Context.Self.Id);
            return Task.CompletedTask;
        }

        public override async Task OnPosition(Position request)
        {
 
        }
    }
}