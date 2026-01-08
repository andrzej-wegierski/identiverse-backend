namespace Domain.Abstractions;

public interface IEmailThrottle
{
    Task<bool> IsAllowedAsync(string key, CancellationToken ct = default);
    Task RegisterAttemptAsync(string key, CancellationToken ct = default);
}
