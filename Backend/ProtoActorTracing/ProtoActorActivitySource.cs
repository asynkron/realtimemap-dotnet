using System.Diagnostics;
using System.Reflection;
using OpenTelemetry.Trace;

namespace Backend.ProtoActorTracing;

internal static class ProtoActorActivitySource
{
    private static readonly AssemblyName AssemblyName = typeof(ProtoActorActivitySource).Assembly.GetName();
    internal static readonly ActivitySource ActivitySource = new(/*AssemblyName.Name!*/ "ProtoActor", AssemblyName.Version!.ToString());
}

public static class TracerProviderBuilderExtensions
{
    /// <summary>
    /// Adds ProtoActor activity source
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static TracerProviderBuilder AddProtoActorInstrumentation(this TracerProviderBuilder builder) 
        => builder.AddSource(ProtoActorActivitySource.ActivitySource.Name);
}