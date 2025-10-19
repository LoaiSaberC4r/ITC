using BuildingBlock.Domain.Results;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Reflection;

namespace BuildingBlock.Application.Behaviors
{
    /// <summary>
    /// Logs non-success outcomes:
    /// - Any Result/Result<T> with IsFailure == true  → Warning/Error log with error codes and estimated HTTP status.
    /// - Any thrown Exception → Error log (then rethrow to be handled by global middleware).
    /// Success results are logged at Debug level (optional toggle).
    /// </summary>
    public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    {
        private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
        private readonly IHttpContextAccessor _http;

        public LoggingBehavior(
            ILogger<LoggingBehavior<TRequest, TResponse>> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _http = httpContextAccessor;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
        {
            var sw = Stopwatch.StartNew();
            var reqName = typeof(TRequest).Name;

            var http = _http.HttpContext;
            var path = http?.Request?.Path.Value ?? "(no-path)";
            var method = http?.Request?.Method ?? "(no-method)";
            var traceId = http?.TraceIdentifier;

            try
            {
                var response = await next();
                sw.Stop();

                if (TryExtractFailure(response, out var errors))
                {
                    var primary = errors.Count > 0 ? errors[0] : Error.Unknown("Unknown", "Unknown failure.");
                    var status = EstimateStatus(primary);
                    var codes = string.Join(",", errors.Select(e => e.Code));
                    var types = string.Join(",", errors.Select(e => e.Type.ToString()));
                    var msgs = string.Join(" | ", errors.Select(e => e.Message));

                    // Use Warning for expected failures; Error for infra/unknown
                    var isInfraOrUnknown = primary.Type is ErrorType.Infrastructure or ErrorType.Unknown;
                    var level = isInfraOrUnknown ? LogLevel.Error : LogLevel.Warning;

                    _logger.Log(level,
                        "Request {Request} FAILED with status {Status} in {ElapsedMs} ms. Codes={Codes} Types={Types} Path={Path} Method={Method} TraceId={TraceId} Msg={Msg}",
                        reqName, status, sw.ElapsedMilliseconds, codes, types, path, method, traceId, msgs);
                }
                else
                {
                    // Optional success log (keep at Debug to reduce noise)
                    _logger.LogDebug("Request {Request} succeeded in {ElapsedMs} ms. Path={Path} Method={Method} TraceId={TraceId}",
                        reqName, sw.ElapsedMilliseconds, path, method, traceId);
                }

                return response;
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex,
                    "Request {Request} threw unhandled exception after {ElapsedMs} ms. Path={Path} Method={Method} TraceId={TraceId}",
                    reqName, sw.ElapsedMilliseconds, path, method, traceId);
                throw;
            }
        }

        private static bool TryExtractFailure(object? response, out IReadOnlyList<Error> errors)
        {
            errors = Array.Empty<Error>();

            if (response is Result r)
            {
                if (r.IsFailure)
                {
                    errors = r.Errors;
                    return true;
                }
                return false;
            }

            var type = response?.GetType();
            if (type is null) return false;

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Result<>))
            {
                // Access IsFailure + Errors via reflection
                var isFailureProp = type.GetProperty("IsFailure", BindingFlags.Public | BindingFlags.Instance);
                var errorsProp = type.GetProperty("Errors", BindingFlags.Public | BindingFlags.Instance);
                if (isFailureProp is null || errorsProp is null) return false;

                var isFailure = (bool)(isFailureProp.GetValue(response) ?? false);
                if (!isFailure) return false;

                var errorsValue = errorsProp.GetValue(response);
                if (errorsValue is IReadOnlyList<Error> errs)
                {
                    errors = errs;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Mirrors ProblemDetailsMapping to estimate HTTP status for logging.
        /// </summary>
        private static int EstimateStatus(Error e) => e.Type switch
        {
            ErrorType.Validation => 422,
            ErrorType.NotFound => 404,
            ErrorType.Conflict => 409,
            ErrorType.Security => 403, // أو 401 تبع حالتك
            ErrorType.RateLimit => 429,
            ErrorType.Infrastructure => 503,
            _ => 500
        };
    }
}