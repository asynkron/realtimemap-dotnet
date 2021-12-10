using System.Diagnostics;
using Backend.Api;
using Backend.Hubs;
using Backend.Infrastructure.Metrics;
using Backend.Infrastructure.Tracing;
using Backend.MQTT;
using Microsoft.AspNetCore.SignalR;
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
builder.Services.AddHostedService<MqttIngress>();

var app = builder.Build();

app.UseCors(b => b
    .WithOrigins("http://localhost:8080")
    .AllowAnyMethod()
    .AllowAnyHeader()
    .AllowCredentials()
);

app.UseRouting();
app.UseOpenTelemetryPrometheusScrapingEndpoint();

app.MapHub<EventsHub>("/events");
app.MapOrganizationApi();
app.MapTrailApi();

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
            .Enrich.WithProperty("service", builder.Configuration["Service:Name"])
            .Enrich.WithProperty("env", builder.Environment.EnvironmentName)
            .Enrich.With<TraceIdEnricher>()
            .ReadFrom.Configuration(context.Configuration));

static void ConfigureTracing(WebApplicationBuilder builder) =>
    builder.Services.AddOpenTelemetryTracing(b =>
        b.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(builder.Configuration["Service:Name"]))
            .AddAspNetCoreInstrumentation(opt => opt.RecordException = true)
            .AddMqttInstrumentation()
            .AddSignalRInstrumentation()
            .AddProtoActorInstrumentation()
            .AddJaegerExporter());

static void ConfigureMetrics(WebApplicationBuilder builder) =>
    builder.Services.AddOpenTelemetryMetrics(b =>
        b   .AddAspNetCoreInstrumentation()
            .AddRealtimeMapInstrumentation()
            .AddPrometheusExporter(poe => poe.ScrapeResponseCacheDurationMilliseconds = 1000)
    );

public class TraceIdEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (Activity.Current != null)
            logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(
                "traceID", Activity.Current.TraceId.ToHexString()));
    }
}