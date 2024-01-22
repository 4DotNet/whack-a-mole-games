namespace Wam.Games.Api.Infrastructure
{
    public class AuthenticationSettings
    {
        /// <summary>
        /// The default Azure AD tenant
        /// </summary>
        public static readonly Guid DefaultTenantId = Guid.Parse("5992a427-2b8e-43f2-a467-7fdc724be4bd");
        /// <summary>
        /// The default domain
        /// </summary>
        public static readonly string DefaultDomain = "4DotNet.nl";
        /// <summary>
        /// The default AD instance URL.
        /// </summary>
        public static readonly string DefaultInstance = "https://login.microsoftonline.com/";
        /// <summary>
        /// Default API scope
        /// </summary>
        public string DefaultScope => $"{ClientId:D}/.default";

        /// <summary>
        /// Default authentication settings for the specified App ID.
        /// </summary>
        /// <param name="appId">The app client id.</param>
        public AuthenticationSettings(Guid clientId)
        {
            ClientId = clientId;
        }

        /// <summary>
        /// Default authentication settings.
        /// <para>Parameterless constructor is required for binding to configuration.</para>
        /// </summary>
        public AuthenticationSettings()
        {
            ClientId = Guid.Empty;
        }

        /// <summary>
        /// The URL of the AAD Instance to use. This is usually Azure AD.
        /// </summary>
        public string Instance { get; set; } = DefaultInstance;
        /// <summary>
        /// The domain name for the AAD tenant.
        /// </summary>
        public string Domain { get; set; } = DefaultDomain;
        /// <summary>
        /// The Azure AD tenant ID
        /// </summary>
        public Guid TenantId { get; set; } = DefaultTenantId;
        /// <summary>
        /// The Client ID of the App Registration for the application.
        /// </summary>
        public Guid ClientId { get; set; }

        public string? ApiScope { get; set; }
        public string? Audience { get; set; }
    }
}