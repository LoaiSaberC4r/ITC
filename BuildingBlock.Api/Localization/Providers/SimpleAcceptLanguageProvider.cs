using BuildingBlock.Api.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Primitives;
using System.Globalization;

namespace BuildingBlock.Api.Localization.Providers
{
    public sealed class SimpleAcceptLanguageProvider : RequestCultureProvider
    {
        private readonly SharedLocalizationOptions _opts;

        public SimpleAcceptLanguageProvider(SharedLocalizationOptions opts) => _opts = opts;

        public override Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
        {
            if (httpContext.Request.Headers.TryGetValue("Accept-Language", out StringValues values))
            {
                // Parse header: e.g. "ar-EG,ar;q=0.9,en-US;q=0.8,en;q=0.7"
                var items = values.ToString()
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(v =>
                    {
                        var parts = v.Split(';', StringSplitOptions.TrimEntries);
                        var tag = parts[0];
                        var q = 1.0;
                        if (parts.Length > 1 && parts[1].StartsWith("q=", StringComparison.OrdinalIgnoreCase))
                            double.TryParse(parts[1][2..], NumberStyles.Any, CultureInfo.InvariantCulture, out q);
                        return (tag, q);
                    })
                    .OrderByDescending(x => x.q)
                    .Select(x => x.tag);

                foreach (var tag in items)
                {
                    // نحاول مطابقة "ar-EG" -> "ar" أو "en-US" -> "en"
                    var two = tag.Length >= 2 ? tag[..2].ToLowerInvariant() : tag.ToLowerInvariant();
                    if (_opts.SupportedCultures.Contains(two, StringComparer.OrdinalIgnoreCase))
                    {
                        return Task.FromResult<ProviderCultureResult?>(new ProviderCultureResult(two));
                    }
                }
            }
            return Task.FromResult<ProviderCultureResult?>(null);
        }
    }
}