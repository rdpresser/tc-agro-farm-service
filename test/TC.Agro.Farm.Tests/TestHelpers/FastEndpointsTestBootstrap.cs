using FastEndpoints;
using Microsoft.Extensions.DependencyInjection;
using TC.Agro.SharedKernel.Infrastructure.Caching.Service;

namespace TC.Agro.Farm.Tests.TestHelpers;

internal static class FastEndpointsTestBootstrap
{
    private static int _initialized;

    public static void EnsureInitialized()
    {
        if (Interlocked.Exchange(ref _initialized, 1) == 0)
        {
            var services = new ServiceCollection();
            services.AddSingleton<ICacheService, NoOpCacheService>();
            Factory.AddServicesForUnitTesting(services);
        }

        Factory.RegisterTestServices(testServices =>
        {
            testServices.AddSingleton<ICacheService, NoOpCacheService>();
        });
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
