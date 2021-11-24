using System.Diagnostics;
using OpenTelemetry.Trace;

namespace Backend.MQTT;

internal static class MqttActivitySource
{
    internal static readonly ActivitySource ActivitySource = new("MQTT", "0.1.0");
}

public static class TracerProviderBuilderExtensions
{
    public static TracerProviderBuilder AddMqttInstrumentation(this TracerProviderBuilder builder)
        => builder.AddSource(MqttActivitySource.ActivitySource.Name);
}