using System.Diagnostics;
using System.Net;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace Backend.ProtoActorTracing;

static class OpenTelemetryHelpers
{
    private static readonly string Hostname = Dns.GetHostName();

    public static void DefaultSetupActivity(Activity span, object message)
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [CanBeNull]
    public static Activity BuildStartedActivity(
        this ActivitySource source,
        string activityName,
        ActivityKind activityKind,
        object message,
        ActivitySetup activitySetup,
        ActivityContext? parentActivityContext = null)
    {
        var messageType = message.MessageName();

        var activity = parentActivityContext != null
            ? source.StartActivity(activityName, activityKind, parentActivityContext.Value)
            : source.StartActivity(activityName, activityKind);


        if (activity != null)
        {
            activity.SetTag(ProtoActorTags.Hostname, Hostname);
            activity.SetTag(ProtoActorTags.MessageType, messageType);
            activitySetup?.Invoke(activity, message);
        }

        return activity;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static string MessageName(this object message) => message?.GetType().Name ?? "null";
}