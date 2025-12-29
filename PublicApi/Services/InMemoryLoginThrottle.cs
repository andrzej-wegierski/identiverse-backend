using System.Collections.Concurrent;
using Domain.Abstractions;

namespace identiverse_backend.Services;

public class InMemoryLoginThrottle : ILoginThrottle
{
    private static readonly ConcurrentDictionary<string, (int Count, DateTimeOffset WindowStart)> Attempts = new();
    private const int WindowSeconds = 60; // 1 minute window
    private const int MaxAttempts = 5;    // allow 5 attempts per window

    public Task<bool> IsAllowedAsync(string key, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var entry = Attempts.GetOrAdd(key, _ => (0, now));
        if ((now - entry.WindowStart).TotalSeconds > WindowSeconds)
        {
            Attempts[key] = (0, now);
            return Task.FromResult(true);
        }
        return Task.FromResult(entry.Count < MaxAttempts);
    }

    public Task RegisterFailureAsync(string key, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        Attempts.AddOrUpdate(key,
            _ => (1, now),
            (_, e) =>
            {
                if ((now - e.WindowStart).TotalSeconds > WindowSeconds)
                    return (1, now);
                return (e.Count + 1, e.WindowStart);
            });
        return Task.CompletedTask;
    }

    public Task RegisterSuccessAsync(string key, CancellationToken ct = default)
    {
        Attempts.TryRemove(key, out _);
        return Task.CompletedTask;
    }
}
