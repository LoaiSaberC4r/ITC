using BuildingBlock.Application.Abstraction;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace BuildingBlock.Infrastracture.Service
{
    public sealed class MemoryCacheService : ICacheService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<MemoryCacheService> _log;

        // tag -> keys
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _tagIndex = new();

        // NEW: تتبّع شامل لكل المفاتيح
        private static readonly ConcurrentDictionary<string, byte> _allKeys = new();

        public Task<(bool found, T? value)> TryGetAsync<T>(string key, CancellationToken ct = default)
        {
            if (_cache.TryGetValue(key, out var obj) && obj is T v)
                return Task.FromResult((true, v));
            return Task.FromResult((false, default(T)));
        }

        public Task SetAsync<T>(string key, T value, TimeSpan ttl, IEnumerable<string> tags, CancellationToken ct = default)
        {
            var options = new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl };
            _cache.Set(key, value!, options);

            _allKeys.TryAdd(key, 0); // NEW

            foreach (var tag in tags ?? Enumerable.Empty<string>())
            {
                var map = _tagIndex.GetOrAdd(tag, _ => new ConcurrentDictionary<string, byte>());
                map.TryAdd(key, 0);
            }
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key, CancellationToken ct = default)
        {
            _cache.Remove(key);
            _allKeys.TryRemove(key, out _); // NEW
            foreach (var (_, map) in _tagIndex)
                map.TryRemove(key, out _);
            return Task.CompletedTask;
        }

        public Task InvalidateByTagsAsync(IEnumerable<string> tags, CancellationToken ct = default)
        {
            var distinct = new HashSet<string>(tags ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            foreach (var tag in distinct)
            {
                if (_tagIndex.TryGetValue(tag, out var keys))
                {
                    foreach (var k in keys.Keys)
                    {
                        _cache.Remove(k);
                        _allKeys.TryRemove(k, out _); // NEW
                    }
                    _tagIndex.TryRemove(tag, out _);
                    _log.LogInformation("Cache invalidated for tag {Tag} ({Count} keys)", tag, keys.Count);
                }
            }
            return Task.CompletedTask;
        }

        // NEW: مسح شامل
        public Task ClearAllAsync(CancellationToken ct = default)
        {
            var count = 0;
            foreach (var k in _allKeys.Keys)
            {
                _cache.Remove(k);
                _allKeys.TryRemove(k, out _);
                count++;
            }
            _tagIndex.Clear();
            _log.LogWarning("Cache cleared (all). Removed {Count} keys.", count);
            return Task.CompletedTask;
        }
    }
}