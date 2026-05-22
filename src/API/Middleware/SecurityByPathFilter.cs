using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace AfriPay.API.Middleware;

public sealed class SecurityByPathFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var path = context.ApiDescription.RelativePath ?? "";

        operation.Security ??= new List<OpenApiSecurityRequirement>();
        
        // AUTH USER (JWT)
        if (path.StartsWith("v1/auth") ||
            path.StartsWith("v1/merchants") ||
            path.StartsWith("v1/team"))
        {
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "JWT"
                        }
                    },
                    Array.Empty<string>()
                }
            });
            return;
        }

        // PAIEMENTS (API KEY)
        if (path.StartsWith("v1/payments", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("v1/refunds", StringComparison.OrdinalIgnoreCase))
        {
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "ApiKey"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        }
    }
}