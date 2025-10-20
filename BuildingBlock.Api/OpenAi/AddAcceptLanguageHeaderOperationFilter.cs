using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace BuildingBlock.Api.OpenAi
{
    public class AddAcceptLanguageHeaderOperationFilter : IOperationFilter
    {
        private readonly List<string> _supportedLanguages = new()
    {
        "en",
        "ar",
    };

        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            operation.Parameters ??= new List<OpenApiParameter>();

            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "Accept-Language",
                In = ParameterLocation.Header,
                Description = "Specify the preferred language for the response",
                Required = false,
                Schema = new OpenApiSchema
                {
                    Type = "string",
                    Enum = _supportedLanguages.Select(lang => new OpenApiString(lang)).Cast<IOpenApiAny>().ToList()
                }
            });
        }
    }
}