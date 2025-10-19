using MediatR;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;
using System.Diagnostics;

namespace BuildingBlock.Application.Behaviors
{
    public sealed class TracingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    {
        private static readonly ActivitySource Source = new("ResultPatternDemo");
        private readonly ILogger<TracingBehavior<TRequest, TResponse>> _logger;

        public TracingBehavior(ILogger<TracingBehavior<TRequest, TResponse>> logger) => _logger = logger;

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
        {
            using var activity = Source.StartActivity(typeof(TRequest).Name, ActivityKind.Server);
            activity?.SetTag("request.type", typeof(TRequest).FullName);
            activity?.SetTag("cancellation", ct.IsCancellationRequested);

            try
            {
                var response = await next();
                activity?.SetTag("result.type", typeof(TResponse).FullName);
                _logger.LogDebug("Handled {RequestType}", typeof(TRequest).Name);
                return response;
            }
            catch (Exception ex)
            {
                activity?.RecordException(ex);
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _logger.LogError(ex, "Unhandled during {RequestType}", typeof(TRequest).Name);
                throw;
            }
        }
    }
}