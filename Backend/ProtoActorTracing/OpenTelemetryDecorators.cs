using System.Diagnostics;
using System.Runtime.CompilerServices;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Trace;

namespace Backend.ProtoActorTracing;

using static ProtoActorTags;
using static ProtoActorActivitySource;

class OpenTelemetryRootContextDecorator : RootContextDecorator
{
    private readonly ActivitySetup _sendSpanSetup;

    public OpenTelemetryRootContextDecorator(IRootContext context, ActivitySetup sendActivitySetup) :
        base(context)
    {
        _sendSpanSetup = (activity, message) =>
        {
            activity.SetTag(ActorType, "<None>");
            sendActivitySetup(activity, message);
        };
    }

    public override void Send(PID target, object message)
        => OpenTelemetryMethodsDecorators.Send(target, message, _sendSpanSetup,
            () => base.Send(target, message));

    public override void Request(PID target, object message)
        => OpenTelemetryMethodsDecorators.Request(target, message, _sendSpanSetup,
            () => base.Request(target, message));

    public override void Request(PID target, object message, PID sender)
        => OpenTelemetryMethodsDecorators.Request(target, message, sender, _sendSpanSetup,
            () => base.Request(target, message, sender));

    public override Task<T> RequestAsync<T>(PID target, object message, CancellationToken cancellationToken)
        => OpenTelemetryMethodsDecorators.RequestAsync(target, message, _sendSpanSetup,
            () => base.RequestAsync<T>(target, message, cancellationToken)
        );
}

class OpenTelemetryActorContextDecorator : ActorContextDecorator
{
    private readonly ActivitySetup _receiveActivitySetup;
    private readonly ActivitySetup _sendActivitySetup;

    public OpenTelemetryActorContextDecorator(IContext context, ActivitySetup sendActivitySetup,
        ActivitySetup receiveActivitySetup) : base(context)
    {
        _sendActivitySetup = (activity, message) =>
        {
            activity.SetTag(ActorType, context.Actor.GetType().Name);
            activity.SetTag(SenderPID, context.Self.ToString());
            sendActivitySetup(activity, message);
        };
        _receiveActivitySetup = (activity, message) =>
        {
            activity.SetTag(ActorType, context.Actor.GetType().Name);
            activity.SetTag(TargetPID, context.Self.ToString());
            receiveActivitySetup(activity, message);
        };
    }

    public override void Send(PID target, object message)
        => OpenTelemetryMethodsDecorators.Send(target, message, _sendActivitySetup,
            () => base.Send(target, message));

    public override Task<T> RequestAsync<T>(PID target, object message, CancellationToken cancellationToken)
        => OpenTelemetryMethodsDecorators.RequestAsync(target, message, _sendActivitySetup,
            () => base.RequestAsync<T>(target, message, cancellationToken)
        );
    
    public override void Request(PID target, object message, PID sender)        
        => OpenTelemetryMethodsDecorators.Request(target, message, sender, _sendActivitySetup,
        () => base.Request(target, message, sender));

    public override void Forward(PID target)
        => OpenTelemetryMethodsDecorators.Forward(target, base.Message, _sendActivitySetup,
            () => base.Forward(target));

    public override Task Receive(MessageEnvelope envelope)
        => OpenTelemetryMethodsDecorators.Receive(envelope, _receiveActivitySetup,
            () => base.Receive(envelope));
}

static class OpenTelemetryMethodsDecorators
{

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Send(PID target, object message, ActivitySetup sendSpanSetup, Action send)
    {
        using var activity =
            ActivitySource.BuildStartedActivity("proto.send " + message.MessageName(), ActivityKind.Producer, message, sendSpanSetup);

        try
        {
            activity?.SetTag(TargetPID, target.ToString());
            send();
        }
        catch (Exception ex)
        {
            activity?.RecordException(ex);
            activity?.SetStatus(ActivityStatusCode.Error);
            throw;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Request(PID target, object message, PID sender, ActivitySetup sendActivitySetup, Action request)
    {
        using var activity =
            ActivitySource.BuildStartedActivity("proto.request " + message.MessageName(), ActivityKind.Client, message, sendActivitySetup);

        try
        {
            activity?.SetTag(TargetPID, target.ToString());
            activity?.SetTag(SenderPID, sender?.ToString() ?? string.Empty);
            request();
        }
        catch (Exception ex)
        {
            activity?.RecordException(ex);
            activity?.SetStatus(ActivityStatusCode.Error);
            throw;
        }
    }    
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Request(PID target, object message, ActivitySetup sendActivitySetup, Action request)
    {
        using var activity =
            ActivitySource.BuildStartedActivity("proto.request " + message.MessageName(), ActivityKind.Client, message, sendActivitySetup);

        try
        {
            activity?.SetTag(TargetPID, target.ToString());
            request();
        }
        catch (Exception ex)
        {
            activity?.RecordException(ex);
            activity?.SetStatus(ActivityStatusCode.Error);
            throw;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static async Task<T> RequestAsync<T>(PID target, object message, ActivitySetup sendActivitySetup,
        Func<Task<T>> requestAsync)
    {
        using var activity =
            ActivitySource.BuildStartedActivity("proto.request " + message.MessageName(), ActivityKind.Client, message, sendActivitySetup);

        try
        {
            activity?.SetTag(TargetPID, target.ToString());
            return await requestAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            activity?.RecordException(ex);
            activity?.SetStatus(ActivityStatusCode.Error);
            throw;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Forward(PID target, object message, ActivitySetup sendSpanSetup, Action forward)
    {
        using var activity =
            ActivitySource.BuildStartedActivity("proto.forward " + message.MessageName(), ActivityKind.Producer, message, sendSpanSetup);

        try
        {
            activity?.SetTag(TargetPID, target.ToString());
            forward();
        }
        catch (Exception ex)
        {
            activity?.RecordException(ex);
            activity?.SetStatus(ActivityStatusCode.Error);
            throw;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static async Task Receive(MessageEnvelope envelope, ActivitySetup receiveSpanSetup,
        Func<Task> receive)
    {
        var message = envelope.Message;

        var parentContext = Propagators.DefaultTextMapPropagator.Extract(
            default,
            envelope.Header,
            (headers, key) => headers.TryGetValue(key, out var value)
                ? new[] { value }
                : Enumerable.Empty<string>());


        using var activity = ActivitySource.BuildStartedActivity(
            "proto.receive " + message.MessageName(), 
            ActivityKind.Server, 
            message,
            receiveSpanSetup, 
            parentContext.ActivityContext);

        try
        {
            if (envelope.Sender != null) activity?.SetTag(SenderPID, envelope.Sender.ToString());

            receiveSpanSetup?.Invoke(activity, message);

            await receive().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            activity?.RecordException(ex);
            activity?.SetStatus(ActivityStatusCode.Error);
            throw;
        }
    }
}