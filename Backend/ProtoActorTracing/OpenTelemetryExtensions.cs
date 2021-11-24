using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;

namespace Backend.ProtoActorTracing;

public delegate void ActivitySetup(Activity activity, object message);

public static class OpenTelemetryExtensions
{
    /// <summary>
    /// Setup open telemetry send middleware & decorator for tracing.
    /// </summary>
    /// <param name="props">props.</param>
    /// <param name="sendActivitySetup">provide a way inject send activity customization according to the message.</param>
    /// <param name="receiveActivitySetup">provide a way inject receive activity customization according to the message.</param>
    /// <returns>props</returns>
    public static Props WithOpenTelemetryTracing(
        this Props props,
        ActivitySetup sendActivitySetup = null,
        ActivitySetup receiveActivitySetup = null
    )
        => props
            .WithContextDecorator(ctx => ctx.WithOpenTelemetryTracing(sendActivitySetup, receiveActivitySetup))
            .WithSenderMiddleware(OpenTelemetrySenderMiddleware());

    /// <summary>
    /// Only responsible to tweak the envelope in order to send ActivityContext information.
    /// </summary>
    public static Func<Sender, Sender> OpenTelemetrySenderMiddleware()
        => next => async (context, target, envelope) =>
        {
            var activity = Activity.Current;

            Task SimpleNext() => next(context, target, envelope); // to forget nothing

            if (activity == null)
            {
                await SimpleNext().ConfigureAwait(false);
            }
            else
            {
                var headers = new Dictionary<string, string>();
                Propagators.DefaultTextMapPropagator.Inject(new PropagationContext(activity.Context, Baggage.Current), headers,
                    (h, key, value) => h[key] = value);

                envelope = envelope.WithHeaders(headers);

                await SimpleNext().ConfigureAwait(false);
            }
        };

    internal static IContext WithOpenTelemetryTracing(
        this IContext context,
        ActivitySetup sendActivitySetup = null,
        ActivitySetup receiveActivitySetup = null
    )
    {
        sendActivitySetup ??= OpenTelemetryHelpers.DefaultSetupActivity;
        receiveActivitySetup ??= OpenTelemetryHelpers.DefaultSetupActivity;

        return new OpenTelemetryActorContextDecorator(context, sendActivitySetup, receiveActivitySetup);
    }

    /// <summary>
    ///     Setup open tracing send decorator around RootContext.
    ///     DO NOT FORGET to create the RootContext passing OpenTelemetryExtensions.OpenTelemetrySenderMiddleware to the
    ///     constructor.
    /// </summary>
    /// <param name="context">the root context</param>
    /// <param name="sendActivitySetup">provide a way inject send activity customization according to the message.</param>
    /// <returns>IRootContext</returns>
    public static IRootContext WithOpenTelemetry(this IRootContext context, ActivitySetup sendActivitySetup = null) 
        => new OpenTelemetryRootContextDecorator(context, sendActivitySetup ?? OpenTelemetryHelpers.DefaultSetupActivity);
}