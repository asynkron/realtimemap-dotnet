using System.Diagnostics.Metrics;
using OpenTelemetry.Metrics;

namespace Backend.Infrastructure.Metrics;

public static class RealtimeMapMetrics
{
    private static readonly Meter RealtimeMapMeter = new("RealtimeMap");

    public static MeterProviderBuilder AddRealtimeMapInstrumentation(this MeterProviderBuilder builder)
        => builder
            .AddMeter("RealtimeMap")
            .AddView("app_mqtt_message_duration", new ExplicitBucketHistogramConfiguration
            {
                Boundaries = Proto.OpenTelemetry.OpenTelemetryMetricsExtensions.RequestLikeHistogramBoundaries
            });

    public static readonly Histogram<double> MqttMessageDuration = RealtimeMapMeter.CreateHistogram<double>(
        "app_mqtt_message_duration",
        description: "Duration of MQTT message processing");

    public static AdjustableGauge SignalRConnections = new(
        RealtimeMapMeter,
        "app_signalr_connections",
        description: "Number of SignalR connections");
}