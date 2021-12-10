using System.Diagnostics;
using OpenTelemetry.Trace;

namespace Backend.Infrastructure.Tracing;

internal static class SignalRActivitySource
{
    internal static readonly ActivitySource ActivitySource = new("SignalR", "0.1.0");
}

public static class SignalRTracingExtensions
{
    public static TracerProviderBuilder AddSignalRInstrumentation(this TracerProviderBuilder builder)
        => builder.AddSource(SignalRActivitySource.ActivitySource.Name);
}