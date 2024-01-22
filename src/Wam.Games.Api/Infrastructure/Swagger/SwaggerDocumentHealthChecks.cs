using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Wam.Games.Api.Infrastructure.Swagger;

/// <summary>
/// Generate Swagger docs for Health end-points.
/// </summary>
internal class SwaggerDocumentHealthChecks : IDocumentFilter
{
    const string HealthStatusSchema = nameof(HealthStatus);
    const string HealthReportEntrySchema = nameof(HealthReportEntry);
    const string HealthReportSchema = nameof(HealthReport);

    public void Apply(OpenApiDocument openApiDocument, DocumentFilterContext context)
    {
        AddHealthStatusSchema(openApiDocument, HealthStatusSchema);
        AddHealthReportEntrySchema(openApiDocument, HealthReportEntrySchema);
        AddHealthReportSchema(openApiDocument, HealthReportSchema);

        var live = new OpenApiPathItem
        {
            Description = "Liveliness check",
            Operations =
            {
                {
                    OperationType.Get, HealthStatusOperation(false)
                }
            }
        };

        openApiDocument.Paths.Add("/health", live);


        var ready = new OpenApiPathItem
        {
            Description = "Readiness check - includes dependencies",
            Operations =
            {
                {
                    OperationType.Get, ReadyOperation(true)
                }
            }
        };

        openApiDocument.Paths.Add("/ready", ready);
    }

    private static OpenApiOperation HealthStatusOperation(bool usesAuth)
    {
        var operation = new OpenApiOperation
        {
            Tags = {new OpenApiTag { Name = "Health" }},
            Responses =
            {
                { "200", HealthStatusResponse(HealthStatus.Healthy) },
                { "503", HealthStatusResponse(HealthStatus.Unhealthy) }
            }
        };

        if (usesAuth)
        {
            operation.Responses.Add("401", new OpenApiResponse { Description = "Authentication required" });
            operation.Responses.Add("403", new OpenApiResponse { Description = "Forbidden" });
        }

        return operation;
    }

    private static OpenApiResponse HealthStatusResponse(HealthStatus exampleValue)
    {
        return new OpenApiResponse
        {
            Description = "Status",
            Content = { { "text/plain", new OpenApiMediaType { Example = new OpenApiString(exampleValue.ToString()), Schema = new OpenApiSchema { Reference = new OpenApiReference { Type = ReferenceType.Schema, Id = HealthStatusSchema } } } } }
        };
    }

    private static OpenApiOperation ReadyOperation(bool usesAuth)
    {
        var operation = new OpenApiOperation
        {
            Tags = {new OpenApiTag { Name = "Health" }},
            Responses =
            {
                { "200", HealthReportResponse() },
                { "503", HealthReportResponse() }
            }
        };

        if (usesAuth)
        {
            operation.Responses.Add("401", new OpenApiResponse { Description = "Authentication required" });
            operation.Responses.Add("403", new OpenApiResponse { Description = "Forbidden" });
        }

        return operation;
    }

    private static OpenApiResponse HealthReportResponse()
    {
        return new OpenApiResponse
        {
            Description = "Status",
            Content = { { "application/json", new OpenApiMediaType { Schema = new OpenApiSchema { Reference = new OpenApiReference { Type = ReferenceType.Schema, Id = HealthReportSchema } } } } }
        };
    }

    private static void AddHealthStatusSchema(OpenApiDocument openApiDocument, string schemaName)
    {
        var healthStatusSchema = new OpenApiSchema
        {
            Description = "Health check status",
            Type = "string",
            Enum =
            {
                new OpenApiString(HealthStatus.Healthy.ToString()),
                new OpenApiString(HealthStatus.Degraded.ToString()),
                new OpenApiString(HealthStatus.Unhealthy.ToString())
            }
        };

        openApiDocument.Components.Schemas.Add(schemaName, healthStatusSchema);
    }

    private static void AddHealthReportEntrySchema(OpenApiDocument openApiDocument, string schemaName)
    {
        var schema = new OpenApiSchema
        {
            Description = "Health report entry",
            Type = "object",
            Nullable = true,
            ReadOnly = true,
            AdditionalPropertiesAllowed = true,
            Properties =
            {
                { "data", new OpenApiSchema{ Description = "Additional data", Type = "object", Nullable = true, ReadOnly = true} },
                { "description", new OpenApiSchema{ Description = "Description", Type = "string" } },
                { "duration", new OpenApiSchema{ Description = "Duration", Type = "string" } },
                { "exception", new OpenApiSchema{ Description = "Exception", Type = "string" } },
                { "status", new OpenApiSchema {Reference = new OpenApiReference{Type = ReferenceType.Schema, Id = HealthStatusSchema}} },
                { "tags", new OpenApiSchema { Type ="array", Items = new OpenApiSchema{ Type = "string" } } }
            }
        };

        openApiDocument.Components.Schemas.Add(schemaName, schema);
    }

    private static void AddHealthReportSchema(OpenApiDocument openApiDocument, string schemaName)
    {
        var schema = new OpenApiSchema
        {
            Description = "Health report",
            Type = "object",
            ReadOnly = true,
            AdditionalPropertiesAllowed = false,
            Properties =
            {
                { "description", new OpenApiSchema{ Description = "Description", Type = "string" } },
                { "totalDuration", new OpenApiSchema{ Description = "Duration", Type = "string" } },
                { "status", new OpenApiSchema {Reference = new OpenApiReference{Type = ReferenceType.Schema, Id = HealthStatusSchema}} },
                { "entries", new OpenApiSchema{ Description = "Entries", Type = "object", AdditionalProperties = new OpenApiSchema{Reference = new OpenApiReference{Type = ReferenceType.Schema, Id = HealthReportEntrySchema}}}},
            }
        };

        openApiDocument.Components.Schemas.Add(schemaName, schema);
    }
}
