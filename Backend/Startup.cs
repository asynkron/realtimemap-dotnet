using System;
using Backend.Actors;
using Backend.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
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
            services.AddGrpc();
            services.AddSingleton(provider =>
            {
                var clusterName = "MyCluster";

                var config = ActorSystemConfig
                    .Setup()
                    .WithDeadLetterThrottleCount(3)
                    .WithDeadLetterThrottleInterval(TimeSpan.FromSeconds(1))
                    .WithDeveloperSupervisionLogging(false);

                var system = new ActorSystem(config);
                
                var assetProps = Props
                    .FromProducer(() => new VehicleActorActor((c, _, _) =>
                        ActivatorUtilities.CreateInstance<VehicleActor>(provider, c)));
                
                system
                    .WithServiceProvider(provider)
                    .WithRemote(GrpcCoreRemoteConfig.BindToLocalhost())
                    .WithCluster(ClusterConfig
                        .Setup(clusterName, new TestProvider(new TestProviderOptions(),new InMemAgent()), new PartitionIdentityLookup())
                        .WithClusterKind("VehicleActor", assetProps)
                    )
                    .Cluster().WithPidCacheInvalidation();

                //hack, start cluster. this should be injected into a hosted service instead
                system.Cluster().StartMemberAsync().GetAwaiter().GetResult();

                return system;
            });

            services.AddSingleton(provider => provider.GetService<ActorSystem>()!.Cluster());
        }
        

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGrpcService<MapBackendService>();

                endpoints.MapGet("/",
                    async context =>
                    {
                        await context.Response.WriteAsync(
                            "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
                    });
            });
        }
    }
}