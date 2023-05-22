using Backend.Actors;
using Backend.Infrastructure.PubSub;
using Google.Protobuf.WellKnownTypes;
using MudBlazor.Services;
using Proto.Cluster.Cache;
using Proto.Cluster.Dashboard;
using Proto.Cluster.Kubernetes;
using Proto.Cluster.Partition;
using Proto.Cluster.PubSub;
using Proto.Cluster.Testing;
using Proto.DependencyInjection;
using Proto.OpenTelemetry;
using Proto.Remote;
using Proto.Remote.GrpcNet;
using Proto.Remote.Healthchecks;
using Proto.Utils;
using StackExchange.Redis;

namespace Backend;

public static class ProtoActorExtensions
{
    public static void AddRealtimeMapProtoActor(this IServiceCollection services)
    {
        services.AddSingleton(provider =>
        {
            var clusterName = "RealtimeMapCluster";
            var config = provider.GetRequiredService<IConfiguration>();

            Log.SetLoggerFactory(provider.GetRequiredService<ILoggerFactory>());

            var actorSystemConfig = ActorSystemConfig
                .Setup()
                .WithMetrics()
                .WithDeadLetterThrottleCount(3)
                .WithDeadLetterThrottleInterval(TimeSpan.FromSeconds(1))
                .WithConfigureRootContext(context => context.WithTracing());

            if (config.GetValue<bool>("ProtoActor:DeveloperLogging"))
            {
                actorSystemConfig
                    .WithDeveloperSupervisionLogging(true)
                    .WithDeadLetterRequestLogging(true)
                    .WithDeveloperThreadPoolStatsLogging(true)
                    .WithDeveloperReceiveLogging(TimeSpan.FromSeconds(5));
                // TODO: check WithConfigureProps to add tracing to each actor
            }

            var system = new ActorSystem(actorSystemConfig);

            var vehicleProps = Props
                .FromProducer(() => new VehicleActorActor((c, _) =>
                    ActivatorUtilities.CreateInstance<VehicleActor>(provider, c)))
                .WithTracing();

            var organizationProps = Props
                .FromProducer(() => new OrganizationActorActor((c, _) =>
                    ActivatorUtilities.CreateInstance<OrganizationActor>(provider, c)))
                .WithTracing();

            IKeyValueStore<Subscribers> kvStore =
                config["ProtoActor:PubSub:SubscribersStorageType"] == "Redis"
                    ? GetRedisSubscribersStore(config)
                    : new InMemoryKeyValueStore();

            var (remoteConfig, clusterProvider) = ConfigureClustering(config);

            system
                .WithServiceProvider(provider)
                .WithRemote(remoteConfig)
                .WithCluster(ClusterConfig
                    .Setup(clusterName, clusterProvider, new PartitionIdentityLookup())
                    // explicit topic actor registration is needed to provide a key value store implementation
                    .WithClusterKind(TopicActor.Kind, Props.FromProducer(() => new TopicActor(kvStore)))
                    .WithClusterKind("VehicleActor", vehicleProps)
                    .WithClusterKind("OrganizationActor", organizationProps)
                )
                .Cluster()
                .WithPidCacheInvalidation();

            return system;
        });

        services.AddSingleton(provider => provider.GetRequiredService<ActorSystem>().Cluster());

        services.AddHostedService<ActorSystemHostedService>();

        services
            .AddHealthChecks()
            .AddCheck<ActorSystemHealthCheck>("actor-system-health");
    }

    private static RedisKeyValueStore GetRedisSubscribersStore(IConfiguration config)
    {
        var multiplexer = ConnectionMultiplexer.Connect(config["ProtoActor:PubSub:RedisConnectionString"]);
        var db = multiplexer.GetDatabase();
        return new RedisKeyValueStore(db, config.GetValue<int>("ProtoActor:PubSub:RedisMaxConcurrency"));
    }

    public static void AddProtoActorDashboard(this IServiceCollection services)
    {
        services.AddServerSideBlazor();
        services.AddRazorPages();
        services.AddMudServices();
        services.AddSingleton(new DashboardSettings());
    }

    public static void MapProtoActorDashboard(this WebApplication app)
    {
        app.UseStaticFiles();
        app.MapBlazorHub();
        app.MapFallbackToPage("/_Host");
    }

    static (GrpcNetRemoteConfig, IClusterProvider) ConfigureClustering(IConfiguration config)
    {
        if (config["ProtoActor:ClusterProvider"] == "Kubernetes")
            return ConfigureForKubernetes(config);

        return ConfigureForLocalhost();
    }

    static (GrpcNetRemoteConfig, IClusterProvider) ConfigureForKubernetes(IConfiguration config)
    {
        var clusterProvider = new KubernetesProvider();

        var remoteConfig = GrpcNetRemoteConfig
            .BindToAllInterfaces(advertisedHost: config["ProtoActor:AdvertisedHost"])
            .WithProtoMessages(EmptyReflection.Descriptor)
            .WithProtoMessages(MessagesReflection.Descriptor)
            .WithLogLevelForDeserializationErrors(LogLevel.Critical)
            .WithRemoteDiagnostics(true); // required by proto.actor dashboard

        return (remoteConfig, clusterProvider);
    }

    static (GrpcNetRemoteConfig, IClusterProvider) ConfigureForLocalhost()
        => (GrpcNetRemoteConfig
                .BindToLocalhost()
                .WithProtoMessages(EmptyReflection.Descriptor)
                .WithProtoMessages(MessagesReflection.Descriptor)
                .WithRemoteDiagnostics(true), // required by proto.actor dashboard
            new TestProvider(new TestProviderOptions(), new InMemAgent()));
}