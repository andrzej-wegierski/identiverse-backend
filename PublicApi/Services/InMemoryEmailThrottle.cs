using System.Collections.Concurrent;
using Domain.Abstractions;

namespace PublicApi.Services;

public class InMemoryEmailThrottle : IEmailThrottle
{
    private readonly ConcurrentDictionary<string, (int Count, DateTimeOffset WindowStart)> _attempts = new();
    private readonly int _maxAttempts = 3;
    private readonly TimeSpan _window = TimeSpan.FromMinutes(10);

    public Task<bool> IsAllowedAsync(string key, CancellationToken ct = default)
    {
        var normalizedKey = key.ToLowerInvariant();
        if (!_attempts.TryGetValue(normalizedKey, out var attempt))
        {
            return Task.FromResult(true);
        }

        if (DateTimeOffset.UtcNow - attempt.WindowStart > _window)
        {
            // Window expired
            return Task.FromResult(true);
        }

        return Task.FromResult(attempt.Count < _maxAttempts);
    }

    public Task RegisterAttemptAsync(string key, CancellationToken ct = default)
    {
        var normalizedKey = key.ToLowerInvariant();
        var now = DateTimeOffset.UtcNow;

        _attempts.AddOrUpdate(
            normalizedKey,
            (Count: 1, WindowStart: now),
            (_, existing) =>
            {
                if (now - existing.WindowStart > _window)
                {
                    return (Count: 1, WindowStart: now);
                }
                return (Count: existing.Count + 1, WindowStart: existing.WindowStart);
            });

        return Task.CompletedTask;
    }
}
