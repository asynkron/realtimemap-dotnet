using System.Diagnostics;
using Backend.Api;
using Backend.Hubs;
using Backend.Infrastructure.Metrics;
using Backend.Infrastructure.Tracing;
using Backend.MQTT;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Proto.OpenTelemetry;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Log = Serilog.Log;

var builder = WebApplication.CreateBuilder(args);

ConfigureLogging(builder);
ConfigureTracing(builder);
ConfigureMetrics(builder);

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddRealtimeMapProtoActor();
builder.Services.AddProtoActorDashboard();
builder.Services.AddHostedService<MqttIngress>();

var app = builder.Build();

app.UseCors(b => b
    .WithOrigins("http://localhost:8080")
    .AllowAnyMethod()
    .AllowAnyHeader()
    .AllowCredentials()
);

// for hosting the proto actor dashboard behind a reverse proxy on a subpath
if (builder.Configuration["PathBase"] != null)
    app.UsePathBase(builder.Configuration["PathBase"]);

app.UseRouting();
app.MapHub<EventsHub>("/events");
app.MapOrganizationApi();
app.MapTrailApi();
app.MapProtoActorDashboard();

try
{
    app.Run();
}
catch (Exception e)
{
    Log.Fatal(e, "Application start-up failed");
}
finally
{
    Log.CloseAndFlush();
}

static void ConfigureLogging(WebApplicationBuilder builder)
    => builder.Host.UseSerilog((context, cfg)
        => cfg
            .ReadFrom.Configuration(context.Configuration)
            .Enrich.WithProperty("service", builder.Configuration["Service:Name"])
            .Enrich.WithProperty("env", builder.Environment.EnvironmentName)
            .Enrich.With<TraceIdEnricher>()
    );

static void ConfigureTracing(WebApplicationBuilder builder) =>
    builder.Services.AddOpenTelemetryTracing(b =>
        b.SetResourceBuilder(ResourceBuilder
                .CreateDefault()
                .AddService(builder.Configuration["Service:Name"])
                // add additional "service" tag to facilitate Grafana traces to logs correlation
                .AddAttributes(new KeyValuePair<string, object>[]
                {
                    new("service", builder.Configuration["Service:Name"]),
                    new("env", builder.Environment.EnvironmentName)
                })
            )
            .AddAspNetCoreInstrumentation(opt => opt.RecordException = true)
            .AddMqttInstrumentation()
            .AddSignalRInstrumentation()
            .AddProtoActorInstrumentation()
            .AddOtlpExporter(opt => { opt.Endpoint = new Uri(builder.Configuration["Otlp:Endpoint"]); }));

static void ConfigureMetrics(WebApplicationBuilder builder) =>
    builder.Services.AddOpenTelemetryMetrics(b =>
        b.SetResourceBuilder(ResourceBuilder
                .CreateDefault()
                .AddService(builder.Configuration["Service:Name"])
            )
            .AddAspNetCoreInstrumentation()
            .AddRealtimeMapInstrumentation()
            .AddProtoActorInstrumentation()
            .AddOtlpExporter(opt =>
            {
                opt.Endpoint = new Uri(builder.Configuration["Otlp:Endpoint"]);
                opt.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds =
                    builder.Configuration.GetValue<int>("Otlp:MetricsIntervalMilliseconds");
            })
    );

public class TraceIdEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (Activity.Current != null)
        {
            // facilitate Grafana logs to traces correlation
            logEvent.AddOrUpdateProperty(
                propertyFactory.CreateProperty("traceID", Activity.Current.TraceId.ToHexString()));
        }
    }
}