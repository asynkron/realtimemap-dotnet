using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Proto;
using Proto.Cluster;

namespace Backend.Actors
{
    public class GlobalViewportActor : GlobalViewportActorBase
    {
        public const string SingleIdentity = "GlobalViewportActor";

        private readonly Dictionary<string, Position> _positionsByVehicleId = new();
        
        public GlobalViewportActor(IContext context) : base(context)
        {
        }

        public override Task OnPosition(Position position)
        {
            _positionsByVehicleId[position.VehicleId] = position;

            return Task.CompletedTask;
        }

        public override Task<PositionBatch> GetPositionsInViewport(Viewport viewport)
        {
            var positions = _positionsByVehicleId
                .Values
                .Where(position => position.IsWithinViewport(viewport))
                .ToArray();

            return Task.FromResult(new PositionBatch
            {
                Positions = { positions }
            });
        }
    }

    public static class GlobalViewportActorContextExtensions
    {
        public static GlobalViewportActorClient GetGlobalViewportActor(this Cluster cluster) =>
            new(cluster, GlobalViewportActor.SingleIdentity);

        public static GlobalViewportActorClient GetGlobalViewportActor(this IContext context) =>
            new(context.System.Cluster(), GlobalViewportActor.SingleIdentity);
    }
}