using Backend.Actors;
using Google.Protobuf.WellKnownTypes;
using k8s;
using Proto.Cluster.Cache;
using Proto.Cluster.Kubernetes;
using Proto.Cluster.Partition;
using Proto.Cluster.Testing;
using Proto.DependencyInjection;
using Proto.OpenTelemetry;
using Proto.Remote;
using Proto.Remote.GrpcCore;

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
                .WithDeadLetterThrottleCount(3)
                .WithDeadLetterThrottleInterval(TimeSpan.FromSeconds(1));

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

            var (remoteConfig, clusterProvider) = ConfigureClustering(config);

            system
                .WithServiceProvider(provider)
                .WithRemote(remoteConfig)
                .WithCluster(ClusterConfig
                    .Setup(clusterName, clusterProvider, new PartitionIdentityLookup())
                    .WithClusterKind("VehicleActor", vehicleProps)
                    .WithClusterKind("OrganizationActor", organizationProps)
                )
                .Cluster()
                .WithPidCacheInvalidation();

            return system;
        });
        
        services.AddSingleton(provider => provider.GetRequiredService<ActorSystem>().Cluster());
        
        services.AddHostedService<ActorSystemHostedService>();
    }

    static (GrpcCoreRemoteConfig, IClusterProvider) ConfigureClustering(IConfiguration config)
    {
        if (config["ProtoActor:ClusterProvider"] == "Kubernetes")
            return ConfigureForKubernetes(config);
        
        return ConfigureForLocalhost();
    }
    
    static (GrpcCoreRemoteConfig, IClusterProvider) ConfigureForKubernetes(IConfiguration config)
    {
        var kubernetes = new Kubernetes(KubernetesClientConfiguration.InClusterConfig());
        var clusterProvider = new KubernetesProvider(kubernetes);

        var host = config["ProtoActor:Host"] ?? "127.0.0.1";

        var remoteConfig = GrpcCoreRemoteConfig
            .BindTo(host)
            .WithProtoMessages(EmptyReflection.Descriptor)
            .WithProtoMessages(MessagesReflection.Descriptor)
            .WithLogLevelForDeserializationErrors(LogLevel.Critical)
            .WithRemoteDiagnostics(true);

        return (remoteConfig, clusterProvider);
    }

    static (GrpcCoreRemoteConfig, IClusterProvider) ConfigureForLocalhost() 
        => (GrpcCoreRemoteConfig.BindToLocalhost(),  new TestProvider(new TestProviderOptions(), new InMemAgent()));
}