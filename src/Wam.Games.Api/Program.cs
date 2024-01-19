using Polly.Extensions.Http;
using Polly;
using Wam.Api.Infrastructure;
using Wam.Api.Infrastructure.Swagger;
using Wam.Core.Configuration;
using Wam.Core.ExtensionMethods;
using Wam.Core.Identity;
using Wam.Games;
using Wam.Games.Services;

var corsPolicyName = "DefaultCors";
var builder = WebApplication.CreateBuilder(args);

var azureCredential = CloudIdentity.GetCloudIdentity();
try
{
    builder.Configuration.AddAzureAppConfiguration(options =>
    {
        var appConfigurationUrl = builder.Configuration.GetRequiredValue("AzureAppConfiguration");
        options.Connect(new Uri(appConfigurationUrl), azureCredential)
            .UseFeatureFlags();
    });
}
catch (Exception ex)
{
    throw new Exception("Failed to configure the Whack-A-Mole Users service, Azure App Configuration failed", ex);
}
// Add services to the container.

builder.Services.AddHttpClient<IUsersService, UsersService>()
    .AddStandardResilienceHandler();


builder.Services
    .AddWamCoreConfiguration(builder.Configuration)
    .AddWamGamesModule();

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: corsPolicyName,
        policy =>
        {
            policy.WithOrigins("http://localhost:4200", "https://app.tinylnk.nl")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
});

builder.Services.AddControllers().AddNewtonsoftJson();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwagger("Whack-A-Mole Users API", enableSwagger: !builder.Environment.IsProduction());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseCors(corsPolicyName);
app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();
app.UseDefaultHealthChecks();
app.MapControllers();

Console.WriteLine("Starting...");
app.Run();
Console.WriteLine("Stopped");

static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        .WaitAndRetryAsync(4, retryAttempt => TimeSpan.FromSeconds(2));
}

static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        .CircuitBreakerAsync(3, TimeSpan.FromSeconds(11));
}