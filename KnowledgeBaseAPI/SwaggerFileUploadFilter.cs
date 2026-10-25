using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace KnowledgeBaseAPI.Api;

public class SwaggerFileUploadFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var fileParams = context.MethodInfo.GetParameters()
            .Where(p => p.ParameterType == typeof(IFormFile));

        if (!fileParams.Any()) return;

        operation.RequestBody = new OpenApiRequestBody
        {
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["multipart/form-data"] = new OpenApiMediaType
                {
                    Schema = new OpenApiSchema
                    {
                        Type = "object",
                        Properties = new Dictionary<string, OpenApiSchema>
                        {
                            ["file"] = new OpenApiSchema { Type = "string", Format = "binary" },
                            ["title"] = new OpenApiSchema { Type = "string" }
                        },
                        Required = new HashSet<string> { "file" }
                    }
                }
            }
        };

        // Remove the auto-generated (broken) parameters for file/title since we've
        // manually described the request body above instead.
        operation.Parameters?.Clear();
    }
}