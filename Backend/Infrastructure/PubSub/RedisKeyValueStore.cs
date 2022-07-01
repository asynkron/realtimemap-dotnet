using Google.Protobuf;
using Proto.Cluster.PubSub;
using Proto.Utils;
using StackExchange.Redis;

namespace Backend.Infrastructure.PubSub;

public class RedisKeyValueStore : ConcurrentKeyValueStore<Subscribers>
{
    private readonly IDatabase _db;

    public RedisKeyValueStore(IDatabase db, int maxConcurrency) : base(new AsyncSemaphore(maxConcurrency)) => _db = db;

    protected override async Task<Subscribers> InnerGetStateAsync(string id, CancellationToken ct)
    {
        var value = await _db.StringGetAsync(Key(id));
        if (value.IsNullOrEmpty)
            return new Subscribers();

        return Subscribers.Parser.ParseFrom(value);
    }

    protected override Task InnerSetStateAsync(string id, Subscribers state, CancellationToken ct)
        => _db.StringSetAsync(Key(id), state.ToByteArray());

    protected override Task InnerClearStateAsync(string id, CancellationToken ct)
        => _db.KeyDeleteAsync(Key(id));

    private string Key(string id) => $"subscribers:{id}";
}