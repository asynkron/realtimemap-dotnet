using System.Collections.Immutable;
using static Backend.Models.Geofences;

namespace Backend.Models
{
    public static class Organizations
    {
        // for showcase purposes, only most active organizations have geofences
        
        public static readonly ImmutableArray<Organization> All = ImmutableArray.Create(new Organization[]
        {
            new("0006", "Oy Pohjolan Liikenne Ab"),
            new("0012", "Helsingin Bussiliikenne Oy", Airport, KallioDistrict, RailwaySquare),
            new("0017", "Tammelundin Liikenne Oy", LauttasaariIsland),
            new("0018", "Pohjolan Kaupunkiliikenne Oy", KallioDistrict, LauttasaariIsland, RailwaySquare),
            new("0020", "Bus Travel Åbergin Linja Oy"),
            new("0021", "Bus Travel Oy Reissu Ruoti"),
            new("0022", "Nobina Finland Oy", Airport, KallioDistrict, LaajasaloIsland),
            new("0030", "Savonlinja Oy", Airport, Downtown),
            new("0036", "Nurmijärven Linja Oy"),
            new("0040", "HKL-Raitioliikenne"),
            new("0045", "Transdev Vantaa Oy"),
            new("0047", "Taksikuljetus Oy"),
            new("0050", "HKL-Metroliikenne"),
            new("0051", "Korsisaari Oy"),
            new("0054", "V-S Bussipalvelut Oy"),
            new("0055", "Transdev Helsinki Oy"),
            new("0058", "Koillisen Liikennepalvelut Oy"),
            new("0060", "Suomenlinnan Liikenne Oy"),
            new("0059", "Tilausliikenne Nikkanen Oy"),
            new("0089", "Metropolia"),
            new("0090", "VR Oy"),
            new("0195", "Siuntio1"),
        });

        public static readonly ImmutableDictionary<string, Organization> ById = All.ToImmutableDictionary(
            keySelector: organization => organization.Id,
            elementSelector: organization => organization);
    }
}