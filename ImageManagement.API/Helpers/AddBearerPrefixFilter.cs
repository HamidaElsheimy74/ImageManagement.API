using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ImageManagement.API.Helpers;

public class AddBearerPrefixFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var securityRequirement = new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    },
                    Name = "Authorization",
                    Description = "Must include 'Bearer ' prefix",
                    Type = SecuritySchemeType.ApiKey,
                    In = ParameterLocation.Header
                },
                Array.Empty<string>()
            }
        };

        operation.Security ??= new List<OpenApiSecurityRequirement>();
        operation.Security.Add(securityRequirement);
    }
}