using BuildingBlock.Domain.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

/// <summary>
/// For Minimum APIs Only Not for MVC Controllers
/// </summary>
namespace BuildingBlock.Api
{
    public static class ProblemDetailsMapping
    {
        public static IResult ToIResult<T>(this Result<T> result, HttpContext http)
        {
            if (result.IsSuccess) return Results.Ok(result.Value);
            return result.Errors.ToProblem(http);
        }

        public static IResult ToIResult(this Result result, HttpContext http)
        {
            if (result.IsSuccess) return Results.NoContent();
            return result.Errors.ToProblem(http);
        }

        public static IResult ToProblem(this IEnumerable<Error> errors, HttpContext http)
        {
            var list = errors.ToList();
            var primary = list.FirstOrDefault() ?? Error.Unknown("Unknown", "An unknown error occurred.");

            var (status, typeUri, title) = Map(primary.Type);

            var pd = new ProblemDetails
            {
                Type = typeUri,
                Title = title,
                Status = (int)status,
                Detail = SafeDetail(primary.Message),
                Instance = http.Request.Path
            };

            // traceId للمساعدة في التحقيق
            pd.Extensions["traceId"] = http.TraceIdentifier;

            // ضمّن أكواد الأخطاء (بدون PII)
            pd.Extensions["errors"] = list.Select(e => new
            {
                code = e.Code,
                message = e.Message,
                type = e.Type.ToString(),
                retryAfter = e.RetryAfter?.TotalSeconds
            });

            if (status == HttpStatusCode.TooManyRequests && primary.RetryAfter is { } ra)
                http.Response.Headers.RetryAfter = ((int)Math.Ceiling(ra.TotalSeconds)).ToString();

            return Results.Problem(pd.Detail, pd.Instance, pd.Status, pd.Title, pd.Type, extensions: pd.Extensions);
        }

        private static (HttpStatusCode status, string typeUri, string title) Map(ErrorType type) => type switch
        {
            ErrorType.Validation => (HttpStatusCode.UnprocessableEntity, "about:blank#validation", "Validation Error"),
            ErrorType.NotFound => (HttpStatusCode.NotFound, "about:blank#not-found", "Resource Not Found"),
            ErrorType.Conflict => (HttpStatusCode.Conflict, "about:blank#conflict", "Conflict"),
            ErrorType.Security => (HttpStatusCode.Forbidden, "about:blank#security", "Security Error"),
            ErrorType.RateLimit => (HttpStatusCode.TooManyRequests, "about:blank#rate-limit", "Too Many Requests"),
            ErrorType.Infrastructure => (HttpStatusCode.ServiceUnavailable, "about:blank#infra", "Infrastructure Error"),
            _ => (HttpStatusCode.InternalServerError, "about:blank#unknown", "Unknown Error")
        };

        private static string SafeDetail(string message)
            => message.Length > 1000 ? message[..1000] + "…" : message;
    }
}