using BuildingBlock.Application.Abstraction;
using BuildingBlock.Domain.Results;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace BuildingBlock.Application.Behaviors
{
    /// <summary>
    /// CacheBehavior (Tag-only strategy)
    /// - Caches only ICacheableQuery<TResponse>.
    /// - On successful ICommand: invalidates ONLY the tags declared by ICacheInvalidator.
    ///   * If a command does not declare tags -> NO invalidation (warning is logged).
    /// Notes:
    /// - By default, we cache only successful Result/Result<T> (configurable via ShouldCache()).
    /// - Cache key is either provided by the query, or auto-built from request type + public props.
    /// </summary>
    public sealed class CacheBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    {
        private readonly ICacheService _cache;
        private readonly ILogger<CacheBehavior<TRequest, TResponse>> _log;

        // Default TTL for cacheable queries when not specified on the request
        private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(5);

        public CacheBehavior(ICacheService cache, ILogger<CacheBehavior<TRequest, TResponse>> log)
        {
            _cache = cache;
            _log = log;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
        {
            // ----------------- QUERY CACHING -----------------
            if (request is ICacheableQuery<TResponse> cacheable)
            {
                var key = cacheable.CacheKey ?? BuildKeyFromRequest(request!);
                var ttl = cacheable.Ttl ?? DefaultTtl;
                var tags = cacheable.Tags ?? Enumerable.Empty<string>();

                var (found, cached) = await _cache.TryGetAsync<TResponse>(key, ct);
                if (found)
                {
                    _log.LogInformation("CACHE HIT  key={Key}", key);
                    return cached!;
                }

                _log.LogInformation("CACHE MISS key={Key}", key);
                var res = await next();

                if (ShouldCache(res))
                {
                    await _cache.SetAsync(key, res, ttl, tags, ct);
                    _log.LogInformation("CACHE SET  key={Key} ttl={Ttl}s tags=[{Tags}]",
                        key, ttl.TotalSeconds, string.Join(",", tags));
                }
                else
                {
                    _log.LogDebug("CACHE SKIP key={Key} (non-success/unsupported result)", key);
                }

                return res;
            }

            // ----------------- COMMAND INVALIDATION (TAG-ONLY) -----------------
            var response = await next();

            if (request is ICommand && IsSuccess(response))
            {
                if (request is ICacheInvalidator invalidator)
                {
                    var tags = (invalidator.Tags ?? Enumerable.Empty<string>())
                               .Where(t => !string.IsNullOrWhiteSpace(t))
                               .Distinct(StringComparer.OrdinalIgnoreCase)
                               .ToArray();

                    if (tags.Length > 0)
                    {
                        await _cache.InvalidateByTagsAsync(tags, ct);
                        _log.LogInformation("CACHE INVALIDATE tags=[{Tags}]", string.Join(",", tags));
                    }
                    else
                    {
                        // Tag-only policy: do NOT clear cache globally.
                        _log.LogWarning("CACHE INVALIDATE skipped: ICommand provided ICacheInvalidator with NO tags. " +
                                        "Tag-only strategy requires commands to declare relevant tags.");
                    }
                }
                else
                {
                    // Tag-only policy: command does not implement ICacheInvalidator -> no invalidation.
                    _log.LogWarning("CACHE INVALIDATE skipped: ICommand does not implement ICacheInvalidator. " +
                                    "Tag-only strategy requires commands to declare tags.");
                }
            }

            return response;
        }

        // ----------------- Helpers -----------------
        private static string BuildKeyFromRequest(object request)
        {
            // Key = FullTypeName|prop1=val1|prop2=val2 ...
            var t = request.GetType();
            var props = t.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            var parts = new List<string> { t.FullName ?? t.Name };
            foreach (var p in props.OrderBy(p => p.Name))
            {
                var v = p.GetValue(request);
                parts.Add($"{p.Name}={SerializeValue(v)}");
            }
            return string.Join("|", parts);
        }

        private static string SerializeValue(object? v)
        {
            if (v is null) return "null";
            return v switch
            {
                string s => s,
                ValueType vt => vt.ToString()!,
                IEnumerable<object> en => $"[{string.Join(",", en.Select(SerializeValue))}]",
                _ => v.ToString() ?? v.GetType().Name
            };
        }

        private static bool ShouldCache(TResponse res)
        {
            // Cache only successful Result/Result<T>; otherwise cache raw DTOs/primitives.
            var type = res?.GetType();
            if (type is null) return true;

            if (type == typeof(Result))
                return ((Result)(object)res!).IsSuccess;

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Result<>))
            {
                var isSuccessProp = type.GetProperty("IsSuccess")!;
                return (bool)(isSuccessProp.GetValue(res) ?? false);
            }

            return true;
        }

        private static bool IsSuccess(TResponse res)
        {
            var type = res?.GetType();
            if (type is null) return true;

            if (type == typeof(Result))
                return ((Result)(object)res!).IsSuccess;

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Result<>))
            {
                var isSuccessProp = type.GetProperty("IsSuccess")!;
                return (bool)(isSuccessProp.GetValue(res) ?? false);
            }

            return true;
        }
    }
}