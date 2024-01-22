using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Identity.Web;

namespace Wam.Games.Api.Infrastructure;

public static class Authentication
{
    /// <summary>
    /// Authenticated related configuration settings.
    /// </summary>
    public static class Settings
    {
        /// <summary>
        /// The name of the setting used to enable authentication: EnableAuthentication
        /// <para>If this setting is not present, it defaults to <c>true</c>.</para>
        /// </summary>
        public const string EnableAuthentication = "EnableAuthentication";
    }

    /// <summary>
    /// Common policy names
    /// </summary>
    public static class Policy
    {
        /// <summary>
        /// The default policy name
        /// </summary>
        public const string Default = "Default";
    }

    /// <summary>
    /// Configures Azure AD authentication for a service.
    /// </summary>
    /// <param name="services">The collection of services.</param>
    /// <param name="configuration">The configuration to use.</param>
    public static void AddAzureAdAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AuthenticationSettings>().BindConfiguration("AzureAD");

        var authenticationEnabled = configuration.GetValue(Settings.EnableAuthentication, true);
        if (!authenticationEnabled)
        {
            // Register a dummy authorization service that overrides any authorization policy
            services.AddTransient<Microsoft.AspNetCore.Authorization.IAuthorizationService, DummyAuthorizationService>();
            return;
        }

        var authSettings = configuration.GetSection("AzureAD").Get<AuthenticationSettings>();
        if (authSettings.ClientId == Guid.Empty)
        {
            throw new InvalidOperationException("Authentication is enabled but ClientId is not supplied");
        }

        if (authSettings.TenantId == Guid.Empty)
        {
            throw new ArgumentException("Authentication is enabled but the TenantId is not supplied",
                nameof(authSettings));
        }

        services.AddAuthentication(options => { options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme; })
            .AddMicrosoftIdentityWebApi(configuration);
    }
    
    /// <summary>
    /// Add a default authorization policy and enforces it with a global filter.
    /// </summary>
    /// <param name="services">The collection of services.</param>
    /// <param name="requiredRoles">Optional: the role(s) that are required by default.</param>
    public static void AddDefaultAuthorizationPolicy(this IServiceCollection services, params string[] requiredRoles)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(Policy.Default, policy =>
            {
                policy.RequireAuthenticatedUser();
                if (requiredRoles.Any())
                {
                    policy.RequireRole(requiredRoles);
                }
            });
        });

        services.Configure<MvcOptions>(options =>
            options.Filters.Add(new AuthorizeFilter(Policy.Default)));
    }
}

/// <summary>
/// Dummy authorization service for local testing only.
/// </summary>
internal class DummyAuthorizationService : IAuthorizationService
{
    public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource, IEnumerable<IAuthorizationRequirement> requirements)
    {
        return Task.FromResult(AuthorizationResult.Success());
    }

    public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource, string policyName)
    {
        return Task.FromResult(AuthorizationResult.Success());
    }
}