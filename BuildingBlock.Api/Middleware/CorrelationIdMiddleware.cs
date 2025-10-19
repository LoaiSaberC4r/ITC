using Microsoft.AspNetCore.Http;
using Serilog.Context;
using System.Diagnostics;

namespace BuildingBlock.Api.Middleware
{
    public sealed class CorrelationIdMiddleware
    {
        public const string CorrelationHeader = "X-Correlation-Id";
        public const string CorrelationItemKey = "__CorrelationId";
        private readonly RequestDelegate _next;

        public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

        public async Task Invoke(HttpContext ctx)
        {
            var corr = ctx.Request.Headers.TryGetValue(CorrelationHeader, out var v)
                ? v.ToString()
                : Guid.NewGuid().ToString("N");

            ctx.Items[CorrelationItemKey] = corr;
            ctx.Response.Headers[CorrelationHeader] = corr;

            var activity = Activity.Current ?? new Activity("IncomingRequest");
            if (activity.Id is null) activity.Start();

            using (LogContext.PushProperty("correlationId", corr))
            using (LogContext.PushProperty("traceId", activity.TraceId.ToString()))
            using (LogContext.PushProperty("spanId", activity.SpanId.ToString()))
            {
                await _next(ctx);
            }
        }
    }
}