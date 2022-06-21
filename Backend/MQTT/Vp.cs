using Newtonsoft.Json;

namespace Backend.MQTT;

public class Root
{
    [JsonProperty("VP")] public Payload? VehiclePosition { get; set; }

    [JsonProperty("DOC")] public Payload? DoorsClosed { get; set; }

    [JsonProperty("DOO")] public Payload? DoorsOpen { get; set; }
}

public class Payload
{
    [JsonProperty("tst")] public DateTimeOffset? Tst { get; set; }

    [JsonProperty("spd")] public double? Spd { get; set; }

    [JsonProperty("hdg")] public long? Hdg { get; set; }

    [JsonProperty("lat")] public double? Lat { get; set; }

    [JsonProperty("long")] public double? Long { get; set; }

    [JsonProperty("drst")] public int? Drst { get; set; }

    public bool HasValidPosition => Lat is not null &&
                                    Long is not null &&
                                    Hdg is not null;
}