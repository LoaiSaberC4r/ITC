using BuildingBlock.Domain.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Net;

namespace BuildingBlock.Api
{
    public static class ProblemDetailsMappingMvc
    {
        // Result<T> → IActionResult
        public static IActionResult ToIActionResult<T>(this Result<T> result, CancellationToken ct = default)
        {
            if (result.IsSuccess) return new OkObjectResult(result.Value);
            return result.Errors.ToActionProblem(ct);
        }

        // Result → IActionResult
        public static IActionResult ToIActionResult(this Result result, CancellationToken ct = default)
        {
            if (result.IsSuccess) return new NoContentResult();
            return result.Errors.ToActionProblem(ct);
        }

        // Errors → IActionResult (ProblemDetails)
        public static IActionResult ToActionProblem(this IEnumerable<Error> errors, CancellationToken ct = default)
            => new ProblemActionResult(errors, ct);

        // ------------ Helpers (same mapping logic) ------------
        internal static (HttpStatusCode status, string typeUri, string title) Map(ErrorType type) => type switch
        {
            ErrorType.Validation => (HttpStatusCode.UnprocessableEntity, "about:blank#validation", "Validation Error"),
            ErrorType.NotFound => (HttpStatusCode.NotFound, "about:blank#not-found", "Resource Not Found"),
            ErrorType.Conflict => (HttpStatusCode.Conflict, "about:blank#conflict", "Conflict"),
            ErrorType.Security => (HttpStatusCode.Forbidden, "about:blank#security", "Security Error"),
            ErrorType.RateLimit => (HttpStatusCode.TooManyRequests, "about:blank#rate-limit", "Too Many Requests"),
            ErrorType.Infrastructure => (HttpStatusCode.ServiceUnavailable, "about:blank#infra", "Infrastructure Error"),
            _ => (HttpStatusCode.InternalServerError, "about:blank#unknown", "Unknown Error")
        };

        internal static string SafeDetail(string message)
            => message.Length > 1000 ? message[..1000] + "…" : message;

        // ------------ IActionResult that builds ProblemDetails at execution time ------------
        private sealed class ProblemActionResult : IActionResult
        {
            private readonly IReadOnlyList<Error> _errors;
            private readonly CancellationToken _ct;

            public ProblemActionResult(IEnumerable<Error> errors, CancellationToken ct)
            {
                _errors = errors.ToList();
                _ct = ct;
            }

            public Task ExecuteResultAsync(ActionContext context)
            {
                var http = context.HttpContext;

                var primary = _errors.FirstOrDefault() ?? Error.Unknown("Unknown", "An unknown error occurred.");
                var (status, typeUri, title) = ProblemDetailsMappingMvc.Map(primary.Type);

                var pd = new ProblemDetails
                {
                    Type = typeUri,
                    Title = title,
                    Status = (int)status,
                    Detail = ProblemDetailsMappingMvc.SafeDetail(primary.Message),
                    Instance = http.Request?.Path // عندنا HttpContext هنا
                };

                // traceId: لو Activity موجود خده، وإلا TraceIdentifier
                var traceId = Activity.Current?.Id ?? http.TraceIdentifier;
                pd.Extensions["traceId"] = traceId;

                // ضمّن أكواد الأخطاء (بدون PII)
                pd.Extensions["errors"] = _errors.Select(e => new
                {
                    code = e.Code,
                    message = e.Message,
                    type = e.Type.ToString(),
                    retryAfter = e.RetryAfter?.TotalSeconds
                });

                // Retry-After header عند 429 + RetryAfter
                if (status == HttpStatusCode.TooManyRequests && primary.RetryAfter is { } ra)
                    http.Response.Headers.RetryAfter = ((int)Math.Ceiling(ra.TotalSeconds)).ToString();

                // نفّذ كـ Problem (يحترم content-type والمخطط)
                var problemIResult = Results.Problem(
                    detail: pd.Detail,
                    instance: pd.Instance,
                    statusCode: pd.Status,
                    title: pd.Title,
                    type: pd.Type,
                    extensions: pd.Extensions);

                // مرّر الـ CancellationToken
                return problemIResult.ExecuteAsync(http);
            }
        }
    }
}