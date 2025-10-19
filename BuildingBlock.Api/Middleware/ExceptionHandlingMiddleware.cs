using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Context;
using System.Net;
using System.Text.Json;

namespace BuildingBlock.Api.Middleware
{
    public sealed class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IWebHostEnvironment _env;

        public ExceptionHandlingMiddleware(RequestDelegate next, IWebHostEnvironment env)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _env = env ?? throw new ArgumentNullException(nameof(env));
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                var traceId = context.TraceIdentifier;
                var corr = context.Items.TryGetValue(CorrelationIdMiddleware.CorrelationItemKey, out var cid)
                    ? cid?.ToString()
                    : null;

                using (LogContext.PushProperty("traceId", traceId))
                using (LogContext.PushProperty("correlationId", corr ?? string.Empty))
                {
                    Log.ForContext("IsAudit", false)
                       .Error(ex, "Unhandled exception for {RequestPath} (corr={correlationId}, trace={traceId})",
                            context.Request.Path, corr, traceId);
                }

                var problem = new
                {
                    type = "https://tools.ietf.org/html/rfc9110#section-15.6.1",
                    title = "An unexpected error occurred.",
                    status = (int)HttpStatusCode.InternalServerError,
                    traceId,
                    correlationId = corr,
                    detail = _env.IsDevelopment() ? ex.ToString() : null
                };

                context.Response.ContentType = "application/problem+json";
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
            }
        }
    }
}