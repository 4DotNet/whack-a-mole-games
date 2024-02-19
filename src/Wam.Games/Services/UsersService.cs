using Dapr.Client;
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
    private readonly ILogger<UsersService> _logger;
    private readonly DaprClient _dapr;
    private readonly Lazy<string> RemoteServiceUrl;
    private const string StateStoreName = "statestore";

    public Task<PlayerDetailsDto?> GetPlayerDetails(Guid userId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting player details from users service {userId}", userId);
        return GetUserFromStateOrService(userId, cancellationToken);
    }

    private async Task<PlayerDetailsDto?> GetUserFromStateOrService(Guid userId, CancellationToken cancellationToken)
    {
        var cacheKey = CacheName.UserDetails(userId);
        var state = await _dapr.GetStateEntryAsync<PlayerDetailsDto>(StateStoreName, cacheKey, cancellationToken: cancellationToken);
        if (state.Value != null)
        {
            return state.Value;
        }
        var playerDetails = await GetPlayerDetailsFromRemoteServer(userId, cancellationToken);
        await _dapr.SaveStateAsync(StateStoreName, cacheKey, playerDetails, cancellationToken: cancellationToken);
        return playerDetails;
    }
    private async Task<PlayerDetailsDto?> GetPlayerDetailsFromRemoteServer(Guid userId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Downloading user information from users service for user {userId}", userId);
        var uri = $"{RemoteServiceUrl.Value}/users/{userId}";
        _logger.LogInformation("Downloading from {userDetailsUrl}", uri);
        var responseString = await _httpClient.GetStringAsync(uri, cancellationToken);
        return JsonConvert.DeserializeObject<PlayerDetailsDto>(responseString);
    }

    private static string RemoteServiceBaseUrl(IOptions<ServicesConfiguration> configuration)
    {
        return $"http://{configuration.Value.UsersService}/api";
    }

    public UsersService(
        HttpClient httpClient, 
        IOptions<ServicesConfiguration> servicesConfiguration,
        ILogger<UsersService> logger,
        DaprClient dapr)
    {
        _httpClient = httpClient;
        _logger = logger;
        _dapr = dapr;
            RemoteServiceUrl = new Lazy<string>(() => RemoteServiceBaseUrl(servicesConfiguration));
    }
}