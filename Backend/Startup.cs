using System;
using Backend.Actors;
using Backend.Hubs;
using Backend.MQTT;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Proto;
using Proto.Cluster;
using Proto.Cluster.Cache;
using Proto.Cluster.Partition;
using Proto.Cluster.Testing;
using Proto.DependencyInjection;
using Proto.Remote.GrpcCore;

namespace Backend
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddSignalR();

            services.AddSingleton(provider =>
            {
                var clusterName = "MyCluster";

                var config = ActorSystemConfig
                    .Setup()
                    .WithDeadLetterThrottleCount(3)
                    .WithDeadLetterThrottleInterval(TimeSpan.FromSeconds(1))
                    .WithDeveloperSupervisionLogging(false);

                var system = new ActorSystem(config);

                var vehicleProps = Props
                    .FromProducer(() => new VehicleActorActor((c, _) =>
                        ActivatorUtilities.CreateInstance<VehicleActor>(provider, c)));

                var organizationProps = Props
                    .FromProducer(() => new OrganizationActorActor((c, _) =>
                        ActivatorUtilities.CreateInstance<OrganizationActor>(provider, c)));

                var globalViewportProps = Props
                    .FromProducer(() => new GlobalViewportActorActor((c, _) =>
                        ActivatorUtilities.CreateInstance<GlobalViewportActor>(provider, c)));

                system
                    .WithServiceProvider(provider)
                    .WithRemote(GrpcCoreRemoteConfig.BindToLocalhost())
                    .WithCluster(ClusterConfig
                        .Setup(clusterName, new TestProvider(new TestProviderOptions(), new InMemAgent()),
                            new PartitionIdentityLookup())
                        .WithClusterKind("VehicleActor", vehicleProps)
                        .WithClusterKind("OrganizationActor", organizationProps)
                        .WithClusterKind("GlobalViewportActor", globalViewportProps)
                    )
                    .Cluster()
                    .WithPidCacheInvalidation();

                return system;
            });

            services.AddSingleton(provider => provider.GetRequiredService<ActorSystem>().Cluster());

            services.AddHostedService<ActorSystemHostedService>();
            services.AddHostedService<MqttIngress>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseCors(builder => builder
                .WithOrigins("http://localhost:8080")
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials()
            );

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<EventsHub>("/events");
                endpoints.MapControllers();
            });
        }
    }
}