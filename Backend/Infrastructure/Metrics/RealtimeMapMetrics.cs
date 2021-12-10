using System.Diagnostics.Metrics;
using OpenTelemetry.Metrics;

namespace Backend.Infrastructure.Metrics;

public static class RealtimeMapMetrics
{
    private static readonly Meter RealtimeMapMeter = new("MQTT");

    public static MeterProviderBuilder AddRealtimeMapInstrumentation(this MeterProviderBuilder builder)
        => builder.AddMeter("MQTT");

    public static readonly Counter<long> MqttMessagesReceived = RealtimeMapMeter.CreateCounter<long>(
        "app_mqtt_messages_received",
        description: "Number of MQTT messages received");
    
    public static AdjustableGauge SignalRConnections = new AdjustableGauge(
        RealtimeMapMeter,
        "app_signalr_connections",
        description: "Number of SignalR connections");
}