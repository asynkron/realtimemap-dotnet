using Backend.Api;
using Backend.Hubs;
using Backend.MQTT;
using Serilog;
using Log = Serilog.Log;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, cfg) 
    => cfg
        .Enrich.FromLogContext()
        .ReadFrom.Configuration(context.Configuration));

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