using System;
using System.Linq;
using System.Threading.Tasks;
using Backend.Models;
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
            var orgId = Context.Self.Id.Substring("partition-activator/".Length, 4);

            var orgName = GetOrganizationName(orgId);

            Console.WriteLine($"Started actor for organization: {orgId} -- {orgName}");

            CreateGeofenceActor(orgId);

            return Task.CompletedTask;
        }

        private string GetOrganizationName(string orgId)
        {
            OrganizationsMap.Data.TryGetValue(orgId, out var orgName);
            if (string.IsNullOrWhiteSpace(orgName)) orgName = orgId;

            return orgName;
        }

        private void CreateGeofenceActor(string orgId)
        {
            var helsinkiAirportLocation = new GeoPoint(24.96907, 60.31146);
            var actorName = $"{orgId}_HelsinkiAirport";
            var geofenceProps = Props.FromProducer(() =>
                new GeofenceActor(actorName, new CircularGeofence(helsinkiAirportLocation, 2000)));

            Context.Spawn(geofenceProps);
        }

        public override Task OnPosition(Position request)
        {
            foreach (var child in Context.Children) Context.Send(child, request);

            return Task.CompletedTask;
        }

        public override async Task<GetGeofencesResponse> GetGeofences(GetGeofencesRequest request)
        {
            var result = new GetGeofencesResponse();
            var tasks = Context.Children.Select(child => Context.RequestAsync<GeofenceDetails>(child, request))
                .ToList();

            foreach (var task in tasks) result.Geofences.Add(await task);

            return result;
        }
    }
}