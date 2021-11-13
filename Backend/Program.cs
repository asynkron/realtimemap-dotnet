using Backend.Api;
using Backend.Hubs;
using Backend.MQTT;
using Serilog;
using Log = Serilog.Log;

var builder = WebApplication.CreateBuilder(args);

ConfigureLogging(builder);

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
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Service", builder.Configuration["Service:Name"])
            .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
            .ReadFrom.Configuration(context.Configuration));