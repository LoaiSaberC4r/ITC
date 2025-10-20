using Microsoft.AspNetCore.Http;
using System.Globalization;

namespace BuildingBlock.Api.Middleware
{
    public sealed class ResponseContentLanguageMiddleware
    {
        private readonly RequestDelegate _next;

        public ResponseContentLanguageMiddleware(RequestDelegate next) => _next = next;

        public async Task Invoke(HttpContext context)
        {
            await _next(context);
            var lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            context.Response.Headers["Content-Language"] = lang;
        }
    }
}