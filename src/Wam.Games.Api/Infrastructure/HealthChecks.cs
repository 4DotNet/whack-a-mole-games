using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Net.Http.Headers;

namespace Wam.Api.Infrastructure;

public static class HealthChecks
{
    /// <summary>
    /// Default health check timeout
    /// </summary>
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(15);

    /// <summary>
    /// Default failure status.
    /// </summary>
    public const HealthStatus DefaultFailureStatus = HealthStatus.Unhealthy;

    public static void UseDefaultHealthChecks(this IApplicationBuilder app)
    {
        app.UseHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions { Predicate = reg => reg.Tags.Contains(HealthTags.Health) });
        app.UseHealthChecks("/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions { Predicate = reg => reg.Tags.Contains(HealthTags.Ready), ResponseWriter = ResponseWriter });
    }

    public static async Task ResponseWriter(HttpContext context, HealthReport report)
    {
        int statusCode;
        switch (report.Status)
        {
            case HealthStatus.Healthy:
                statusCode = (int)HttpStatusCode.OK;
                break;
            default:
                statusCode = (int)HttpStatusCode.ServiceUnavailable;
                break;
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        // prevent caching of responses
        var headers = context.Response.Headers;
        headers[HeaderNames.CacheControl] = "no-store, no-cache";
        headers[HeaderNames.Pragma] = "no-cache";
        headers[HeaderNames.Expires] = "Thu, 01 Jan 1970 00:00:00 GMT";

        await context.Response.WriteAsJsonAsync(report, SerializerOptions);
    }

    private sealed class TimeSpanConverter : JsonConverter<TimeSpan>
    {
        public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return new TimeSpan();
        }

        public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }

    private readonly static JsonSerializerOptions SerializerOptions = new JsonSerializerOptions
    {
        AllowTrailingCommas = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(), new TimeSpanConverter() }
    };
}
public static class HealthTags
{
    /// <summary>
    /// The tag for ready checks. Use this tag for checks that call out-of-process resources.
    /// </summary>
    public const string Ready = "ready";
    /// <summary>
    /// The tag for health checks (liveliness). Use this for internal resource checks only.
    /// </summary>
    public const string Health = "health";
}