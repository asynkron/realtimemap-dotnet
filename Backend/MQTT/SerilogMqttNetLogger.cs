using MQTTnet.Diagnostics.Logger;
using Serilog.Core;
using Serilog.Events;
using Log = Serilog.Log;

namespace Backend.MQTT;

public class SerilogMqttNetLogger : IMqttNetLogger
{
    private readonly string _source;
    public SerilogMqttNetLogger(string source = null) => _source = source ?? "MQTT";

    public void Publish(
        MqttNetLogLevel logLevel,
        string source,
        string message,
        object[] parameters,
        Exception exception)
    {
        Log.ForContext(Constants.SourceContextPropertyName, _source)
            // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
            .Write(MapLogLevel(logLevel), exception, message, parameters);
    }

    private LogEventLevel MapLogLevel(MqttNetLogLevel logLevel)
        => logLevel switch
        {
            MqttNetLogLevel.Verbose => LogEventLevel.Debug,
            MqttNetLogLevel.Info => LogEventLevel.Information,
            MqttNetLogLevel.Warning => LogEventLevel.Warning,
            MqttNetLogLevel.Error => LogEventLevel.Error,
            _ => LogEventLevel.Verbose
        };
}