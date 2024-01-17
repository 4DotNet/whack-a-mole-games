using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Swagger;

namespace Wam.Api.Infrastructure.Swagger;

public class ConfigureSwaggerOptions : IConfigureNamedOptions<SwaggerOptions>
{
    public void Configure(string name, SwaggerOptions options)
    {
        // Use proxy headers to fix the url offset introduced by the reverse proxy
        options.PreSerializeFilters.Add((swagger, httpReq) =>
        {
            if (httpReq.Headers.ContainsKey("X-Forwarded-Host"))
            {
                var serverUrl = $"https://{httpReq.Headers["X-Forwarded-Host"]}/api";
                swagger.Servers = new List<OpenApiServer> { new OpenApiServer { Url = serverUrl } };
            }
        });
    }

    public void Configure(SwaggerOptions options)
    {
        Configure(Options.DefaultName, options);
    }
}
