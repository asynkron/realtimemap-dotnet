using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Backend.Models;
using Proto;

namespace Backend.Actors
{
    public class OrganizationActor : OrganizationActorBase
    {
        private static readonly Dictionary<string, string> OrganizationsMap = new()
        {
            {"0006", "Oy Pohjolan Liikenne Ab"},
            {"0012", "Helsingin Bussiliikenne Oy"},
            {"0017", "Tammelundin Liikenne Oy"},
            {"0018", "Pohjolan Kaupunkiliikenne Oy"},
            {"0020", "Bus Travel Åbergin Linja Oy"},
            {"0021", "Bus Travel Oy Reissu Ruoti"},
            {"0022", "Nobina Finland Oy"},
            {"0030", "Savonlinja Oy"},
            {"0036", "Nurmijärven Linja Oy"},
            {"0040", "HKL-Raitioliikenne"},
            {"0045", "Transdev Vantaa Oy"},
            {"0047", "Taksikuljetus Oy"},
            {"0050", "HKL-Metroliikenne"},
            {"0051", "Korsisaari Oy"},
            {"0054", "V-S Bussipalvelut Oy"},
            {"0055", "Transdev Helsinki Oy"},
            {"0058", "Koillisen Liikennepalvelut Oy"},
            {"0060", "Suomenlinnan Liikenne Oy"},
            {"0059", "Tilausliikenne Nikkanen Oy"},
            {"0089", "Metropolia"},
            {"0090", "VR Oy"},
            {"0195", "Siuntio1"}
        };


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
            OrganizationsMap.TryGetValue(orgId, out var orgName);
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
    }
}