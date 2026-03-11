using FastEndpoints;
using Microsoft.Extensions.DependencyInjection;
using TC.Agro.SharedKernel.Infrastructure.Caching.Service;

namespace TC.Agro.Farm.Tests.TestHelpers;

internal static class FastEndpointsTestBootstrap
{
    private static readonly object SyncLock = new();
    private static bool _initialized;

    public static void EnsureInitialized()
    {
        if (_initialized)
        {
            return;
        }

        lock (SyncLock)
        {
            if (_initialized)
            {
                return;
            }

            Factory.RegisterTestServices(testServices =>
            {
                testServices.AddLogging();
                testServices.AddSingleton<ICacheService, NoOpCacheService>();
            });

            _initialized = true;
        }
    }

    private sealed class NoOpCacheService : ICacheService
    {
        public Task<T?> GetAsync<T>(
            string key,
            TimeSpan? duration = null,
            TimeSpan? distributedCacheDuration = null,
            CancellationToken cancellationToken = default)
            => Task.FromResult<T?>(default);

        public Task<T?> GetOrSetAsync<T>(
            string key,
            Func<CancellationToken, Task<T>> factory,
            TimeSpan? duration = null,
            TimeSpan? distributedCacheDuration = null,
            CancellationToken cancellationToken = default)
            => Task.FromResult<T?>(default);

        public Task SetAsync<T>(
            string key,
            T value,
            TimeSpan? duration = null,
            TimeSpan? distributedCacheDuration = null,
            IReadOnlyCollection<string>? tags = null,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task RemoveAsync(
            string key,
            TimeSpan? duration = null,
            TimeSpan? distributedCacheDuration = null,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task RemoveByTagAsync(
            string tag,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task RemoveByTagAsync(
            IEnumerable<string> tags,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
