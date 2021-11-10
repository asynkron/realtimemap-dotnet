using Newtonsoft.Json;

namespace Backend.MQTT;

public class Root
{
    [JsonProperty("VP")] public Payload VehiclePosition { get; set; }

    [JsonProperty("DOC")] public Payload DoorsClosed { get; set; }

    [JsonProperty("DOO")] public Payload DoorsOpen { get; set; }
}

public class Payload
{
    [JsonProperty("desi")] public string Desi { get; set; }

    [JsonProperty("dir")] public string Dir { get; set; }

    [JsonProperty("oper")] public long? Oper { get; set; }

    [JsonProperty("veh")] public long? Veh { get; set; }

    [JsonProperty("tst")] public DateTimeOffset? Tst { get; set; }

    [JsonProperty("tsi")] public long? Tsi { get; set; }

    [JsonProperty("spd")] public double? Spd { get; set; }

    [JsonProperty("hdg")] public long? Hdg { get; set; }

    [JsonProperty("lat")] public double? Lat { get; set; }

    [JsonProperty("long")] public double? Long { get; set; }

    [JsonProperty("acc")] public double? Acc { get; set; }

    [JsonProperty("dl")] public long? Dl { get; set; }

    [JsonProperty("odo")] public long? Odo { get; set; }

    [JsonProperty("drst")] public int? Drst { get; set; }

    [JsonProperty("oday")] public DateTimeOffset? Oday { get; set; }

    [JsonProperty("jrn")] public long? Jrn { get; set; }

    [JsonProperty("line")] public long? Line { get; set; }

    [JsonProperty("start")] public string Start { get; set; }

    [JsonProperty("loc")] public string Loc { get; set; }

    [JsonProperty("stop")] public string Stop { get; set; }

    [JsonProperty("route")] public string Route { get; set; }

    [JsonProperty("occu")] public long? Occu { get; set; }

    public bool HasValidPosition => Lat is not null &&
                                    Long is not null &&
                                    Hdg is not null;
}