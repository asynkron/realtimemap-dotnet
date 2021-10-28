using System;
using System.Linq;
using System.Threading.Tasks;
using Backend.Models;
using Proto;

namespace Backend.Actors
{
    public class OrganizationActor : OrganizationActorBase
    {
        private string _organizationName;
        
        public OrganizationActor(IContext context) : base(context)
        {
        }

        public override Task OnStarted()
        {
            var organizationId = Context.Self.Id.Substring("partition-activator/".Length, 4);

            var organization = GetOrganization(organizationId);

            _organizationName = organization.Name;

            Console.WriteLine($"Started actor for organization: {organizationId} -- {_organizationName}");

            foreach (var geofence in organization.Geofences)
            {
                CreateGeofenceActor(geofence);
            }

            return Task.CompletedTask;
        }

        private static Organization GetOrganization(string organizationId)
        {
            return Organizations.ById.TryGetValue(organizationId, out var foundOrganization)
                ? foundOrganization
                : new Organization(organizationId, organizationId);
        }

        private void CreateGeofenceActor(CircularGeofence circularGeofence)
        {
            var geofenceProps = Props.FromProducer(() => new GeofenceActor(
                _organizationName,
                circularGeofence,
                Cluster
            ));

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
            
            var tasks = Context.Children
                .Select(child => Context.RequestAsync<GeofenceDetails>(child, request))
                .ToList();

            foreach (var task in tasks) result.Geofences.Add(await task);

            return result;
        }
    }
}