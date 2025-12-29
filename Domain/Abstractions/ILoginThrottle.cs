using System.Threading;
using System.Threading.Tasks;

namespace Domain.Abstractions;

public interface ILoginThrottle
{
    Task<bool> IsAllowedAsync(string key, CancellationToken ct = default);
    Task RegisterFailureAsync(string key, CancellationToken ct = default);
    Task RegisterSuccessAsync(string key, CancellationToken ct = default);
}
