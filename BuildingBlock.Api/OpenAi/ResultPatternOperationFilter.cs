using BuildingBlock.Application.Abstraction;
using BuildingBlock.Domain.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace BuildingBlock.Api.OpenAi
{
    public sealed class ResultPatternOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            // لو فيه ProducesResponseType محدد يدويًا، بلاش نتدخل
            if (operation.Responses?.Count > 0 && operation.Responses.Keys.Any(k => k != "default"))
                return;

            var successType = TryGetSuccessTypeFromAction(context);

            // سجّل ProblemDetails للـ errors القياسية بتاعت Result Pattern
            AddProblem(operation, context, 422, "Unprocessable Entity");
            AddProblem(operation, context, 404, "Not Found");
            AddProblem(operation, context, 409, "Conflict");
            AddProblem(operation, context, 403, "Forbidden");
            AddProblem(operation, context, 429, "Too Many Requests");
            AddProblem(operation, context, 500, "Internal Server Error");

            // لو عرفنا نوع النجاح (T)، ضيف 200 بـ schema = T
            if (successType != null && successType != typeof(void))
            {
                var schema = context.SchemaGenerator.GenerateSchema(successType, context.SchemaRepository);
                operation.Responses["200"] = new OpenApiResponse
                {
                    Description = "OK",
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["application/json"] = new OpenApiMediaType { Schema = schema }
                    }
                };
            }
            else
            {
                // لو مفيش T (Result بدون قيمة) خليه 204
                operation.Responses["204"] = new OpenApiResponse { Description = "No Content" };
            }
        }

        private static void AddProblem(OpenApiOperation op, OperationFilterContext ctx, int status, string desc)
        {
            var schema = ctx.SchemaGenerator.GenerateSchema(typeof(ProblemDetails), ctx.SchemaRepository);
            op.Responses[status.ToString()] = new OpenApiResponse
            {
                Description = desc,
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["application/problem+json"] = new OpenApiMediaType { Schema = schema }
                }
            };
        }

        private static Type? TryGetSuccessTypeFromAction(OperationFilterContext context)
        {
            // 1) لو الأكشن بيرجع ActionResult<T> أو T
            var method = (context.ApiDescription.ActionDescriptor as Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor)?.MethodInfo;
            var returnType = method?.ReturnType;

            // unwrap Task<>
            if (returnType is not null && typeof(System.Threading.Tasks.Task).IsAssignableFrom(returnType))
                returnType = returnType.IsGenericType ? returnType.GetGenericArguments()[0] : typeof(void);

            // ActionResult<T> → T
            if (returnType is not null && IsActionResultOfT(returnType, out var art))
                return art;

            // لو الرجوع IResult/IActionResult هنحاول نستنتج من باراميترات الـ CQRS
            var paramTypes = context.MethodInfo.GetParameters().Select(p => p.ParameterType);
            foreach (var pt in paramTypes)
            {
                // هل هو IQuery<Result<T>> ؟
                if (TryGetQueryResultOfT(pt, out var t))
                    return t;

                // بديل: بعض الأوامر ممكن ترجع Result<T> برضه (لو GET misused)
                if (TryGetCommandResultOfT(pt, out var t2))
                    return t2;
            }

            // كمان: لو فيه Handler معروف (مش هنقفّش عليه هنا لتقليل التعقيد)
            return null;
        }

        private static bool IsActionResultOfT(Type type, out Type? inner)
        {
            inner = null;
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ActionResult<>))
            {
                inner = type.GetGenericArguments()[0];
                return true;
            }
            return false;
        }

        private static bool TryGetQueryResultOfT(Type candidate, out Type? t)
        {
            t = null;
            // يدعم records/classes اللي بتنفذ IQuery<Result<T>>
            foreach (var it in candidate.GetInterfaces())
            {
                if (it.IsGenericType && it.GetGenericTypeDefinition() == typeof(IQuery<>))
                {
                    var arg = it.GetGenericArguments()[0];
                    if (IsResultOfT(arg, out var inner))
                    {
                        t = inner;
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool TryGetCommandResultOfT(Type candidate, out Type? t)
        {
            t = null;
            foreach (var it in candidate.GetInterfaces())
            {
                if (it.IsGenericType && it.GetGenericTypeDefinition() == typeof(ICommand<>))
                {
                    var arg = it.GetGenericArguments()[0];
                    if (IsResultOfT(arg, out var inner))
                    {
                        t = inner;
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool IsResultOfT(Type type, out Type? inner)
        {
            inner = null;
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Result<>))
            {
                inner = type.GetGenericArguments()[0];
                return true;
            }
            return false;
        }
    }
}