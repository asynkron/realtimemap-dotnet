using System.Diagnostics;
using OpenTelemetry.Trace;

namespace Backend.Hubs;

internal static class SignalRActivitySource
{
    internal static readonly ActivitySource ActivitySource = new("SignalR", "0.1.0");
}

public static class TracerProviderBuilderExtensions
{
    public static TracerProviderBuilder AddSignalRInstrumentation(this TracerProviderBuilder builder)
        => builder.AddSource(SignalRActivitySource.ActivitySource.Name);
}