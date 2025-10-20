using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace BuildingBlock.Api.OpenAi
{
    public class AuthorizeCheckOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var hasAuthorize = context.MethodInfo.DeclaringType!.GetCustomAttributes(true)
                .Union(context.MethodInfo.GetCustomAttributes(true))
                .OfType<AuthorizeAttribute>()
                .Any();

            if (!hasAuthorize)
            {
                return;
            }

            var authorizeAttributes = context.MethodInfo.DeclaringType!.GetCustomAttributes(true)
                .Union(context.MethodInfo.GetCustomAttributes(true))
                .OfType<AuthorizeAttribute>();

            var allowedRoles = authorizeAttributes
                .Select(attr => attr.Roles)
                .Where(roles => !string.IsNullOrWhiteSpace(roles))
                .SelectMany(roles => roles!.Split(','))
                .Distinct();

            if (allowedRoles.Any())
            {
                operation.Description = $"Required roles: {string.Join(", ", allowedRoles)}";
            }
        }
    }
}