using System.Diagnostics;
using OpenTelemetry.Trace;

namespace Backend.Infrastructure.Tracing;

internal static class MqttActivitySource
{
    internal static readonly ActivitySource ActivitySource = new("MQTT");
}

public static class MqttTracingExtensions
{
    public static TracerProviderBuilder AddMqttInstrumentation(this TracerProviderBuilder builder)
        => builder.AddSource(MqttActivitySource.ActivitySource.Name);
}