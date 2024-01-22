using HexMaster.RedisCache.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Wam.Core.Cache;
using Wam.Core.Configuration;
using Wam.Games.DataTransferObjects;

namespace Wam.Games.Services;

public class UsersService : IUsersService
{
    private readonly HttpClient _httpClient;
    private readonly IOptions<ServicesConfiguration> _servicesConfiguration;
    private readonly ICacheClientFactory _cacheClientFactory;
    private readonly ILogger<UsersService> _logger;
    private readonly string _remoteServiceBaseUrl;

    public Task<PlayerDetailsDto?> GetPlayerDetails(Guid userId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting player details from users service {userId}", userId);
        var cacheClient = _cacheClientFactory.CreateClient();
        var cacheKey = CacheName.UserDetails(userId);
        return cacheClient.GetOrInitializeAsync(() => GetPlayerDetailsFromRemoteServer(userId, cancellationToken), cacheKey);
    }

    public async Task<PlayerDetailsDto?> BanUser(Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Make a user ban request to the users API {userId}", userId);
            var uri = $"{_remoteServiceBaseUrl}/Users/{userId}/Ban";
            _logger.LogInformation("Making request to {userDetailsUrl}", uri);
            var responseString = await _httpClient.GetStringAsync(uri, cancellationToken);
            return JsonConvert.DeserializeObject<PlayerDetailsDto>(responseString);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while making a user ban request to the users API {userId}", userId);
            throw;
        }
    }

    private async Task<PlayerDetailsDto?> GetPlayerDetailsFromRemoteServer(Guid userId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Downloading user information from users service for user {userId}", userId);
        var uri = $"{_remoteServiceBaseUrl}/users/{userId}";
        _logger.LogInformation("Downloading from {userDetailsUrl}", uri);
        var responseString = await _httpClient.GetStringAsync(uri, cancellationToken);
        return JsonConvert.DeserializeObject<PlayerDetailsDto>(responseString);
    }

    private string RemoteServiceBaseUrl()
    {
        return $"http://{_servicesConfiguration.Value.UsersService}/api";
    }

    public UsersService(
        HttpClient httpClient, 
        IOptions<ServicesConfiguration> servicesConfiguration,
        ICacheClientFactory cacheClientFactory,
        ILogger<UsersService> logger)
    {
        _httpClient = httpClient;
        _servicesConfiguration = servicesConfiguration;
        _cacheClientFactory = cacheClientFactory;
        _logger = logger;
        _remoteServiceBaseUrl = RemoteServiceBaseUrl();
    }
}