using System.Collections.Concurrent;
using Proto.Cluster.PubSub;

namespace Backend.Actors;

// share batching producer instances across actors
// use the BatchingProducer over direct Cluster.Publisher().Publish() to improve performance
// when many sources are publishing to the same topic at a fast rate
public static class SharedProducers
{
    private static readonly ConcurrentDictionary<string, Lazy<BatchingProducer>> Producers = new();

    public static BatchingProducer GetProducer(Cluster cluster, string topic) =>
        Producers.GetOrAdd(
                topic,
                new Lazy<BatchingProducer>(
                    () => new BatchingProducer(cluster.Publisher(), topic, new BatchingProducerConfig
                    {
                        MaxQueueSize = 10000,
                        OnPublishingError = (_, _, _) => Task.FromResult(PublishingErrorDecision.FailBatchAndContinue)
                    }), true)
            )
            .Value;
}