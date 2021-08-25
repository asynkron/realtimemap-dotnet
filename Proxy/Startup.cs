using System;
using Backend;
using Grpc.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Proxy.Hubs;
using Proxy.Notifications;

namespace Proxy
{
    public class Startup
    {
        public Startup(IConfiguration configuration) => Configuration = configuration;

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            
            services.AddControllers();
            services.AddSignalR();
            services.AddGrpcClient<MapBackend.MapBackendClient>(o =>
            {
                o.Address = new Uri("http://127.0.0.1:5002");
            }).ConfigureChannel(c =>
            {
                c.Credentials = ChannelCredentials.Insecure;
            });

            services.AddHostedService<NotificationsHostedService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment()) app.UseDeveloperExceptionPage();

            app.UseCors(builder =>
                builder
                    .WithOrigins("http://localhost:8080")
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials()
            );

         //   app.UseHttpsRedirection();

            app.UseRouting();

         //   app.UseAuthorization();

            app.UseEndpoints(endpoints => {
                    endpoints.MapControllers();
                    endpoints.MapHub<PositionsHub>("/positionhub");
                }
            );
        }
    }
}