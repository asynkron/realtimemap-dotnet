using Backend.Api;
using Backend.Hubs;
using Backend.MQTT;

var builder = WebApplication.CreateBuilder(args);

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

app.Run();