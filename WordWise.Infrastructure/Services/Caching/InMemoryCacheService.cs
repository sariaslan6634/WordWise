using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WordWise.Application.Common.Interfaces;

namespace WordWise.Infrastructure.Services.Caching
{
    public class InMemoryCacheService : ICacheService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger _logger;

        public static readonly ConcurrentDictionary<string, byte> _keyRegistry = new();
        public static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(30);
        public InMemoryCacheService(ILogger logger, IMemoryCache cache)
        {
            _logger = logger;
            _cache = cache;
        }




        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
        {
            var found = _cache.TryGetValue(key, out T? value);

            _logger.LogDebug(found ? "Cache HIT: {Key}" : "Cache MISS: {Key}", key);

            return Task.FromResult(value);
        }

        public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
        {
            var cached = await GetAsync<T>(key, cancellationToken);
            if (cached is not null)
                return cached;

            var value = await factory();
            if (value is not null)
                await SetAsync(key, value, expiration, cancellationToken);

            return value;
        }

        public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            _cache.Remove(key);
            _keyRegistry.TryRemove(key, out _);
            _logger.LogDebug("Cache REMOVE: {Key}", key);

            return Task.CompletedTask;
        }

        public Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
        {
            var keysToRemove = _keyRegistry.Keys.Where(x => x.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList();

            foreach (var key in keysToRemove)
            {
                _cache.Remove(key);
                _keyRegistry.TryRemove(key, out _);
            }
            _logger.LogDebug("Cache REMOVE BY PREFIX: {Prefix}, {Count} keys removed", prefix, keysToRemove.Count);

            return Task.CompletedTask;
        }

        public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
        {
            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? DefaultExpiration
            };

            options.RegisterPostEvictionCallback((evictedKey, _, _, _) =>
            {
                _keyRegistry.TryRemove(evictedKey.ToString()!, out _);
            });

            _cache.Remove(key);
            _keyRegistry.TryRemove(key, out _);
            _logger.LogDebug("Cache REMOVE: {Key}", key);
            return Task.CompletedTask;
        }
    }
}
