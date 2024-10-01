using Dapr.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Wam.Core.Abstractions;
using Wam.Core.Cache;
using Wam.Core.Configuration;
using Wam.Games.DataTransferObjects;

namespace Wam.Games.Services;

public class UsersService(
    IOptions<ServicesConfiguration> servicesConfiguration,
    ILogger<UsersService> logger,
    IWamCacheService cacheService,
    DaprClient dapr)
    : IUsersService
{
    private const string StateStoreName = "statestore";


    public Task<PlayerDetailsDto?> GetPlayerDetails(Guid userId, CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting player details from users service {userId}", userId);
        var cacheKey = CacheName.UserDetails(userId);
        return cacheService.GetFromCacheOrInitialize(cacheKey,
            () => GetPlayerDetailsFromRemoteServer(userId, cancellationToken), 600, cancellationToken);
    }

    //private async Task<PlayerDetailsDto?> GetUserFromStateOrService(Guid userId, CancellationToken cancellationToken)
    //{


    //    var state = await dapr.GetStateEntryAsync<PlayerDetailsDto>(StateStoreName, cacheKey, cancellationToken: cancellationToken);
    //    if (state.Value != null)
    //    {
    //        return state.Value;
    //    }
    //    var playerDetails = await GetPlayerDetailsFromRemoteServer(userId, cancellationToken);
    //    await dapr.SaveStateAsync(StateStoreName, cacheKey, playerDetails, cancellationToken: cancellationToken);
    //    return playerDetails;
    //}
    private async Task<PlayerDetailsDto?> GetPlayerDetailsFromRemoteServer(Guid userId, CancellationToken cancellationToken)
    {
        logger.LogInformation("Downloading user information from users service for user {userId}", userId);
        var daprClientResponse = await dapr.InvokeMethodAsync< PlayerDetailsDto>(
            HttpMethod.Get,
            servicesConfiguration.Value.VouchersService, 
            $"api/users/{userId}",
        cancellationToken);

        return daprClientResponse;
    }
}