using Azure.Core;
using Azure.Messaging.WebPubSub;
using Microsoft.Extensions.Logging;
using Dapr.Client;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;
using Wam.Core.Cache;
using Wam.Core.Events;
using Wam.Games.DataTransferObjects;
using Wam.Games.DomainModels;
using Wam.Games.ErrorCodes;
using Wam.Games.EventData;
using Wam.Games.Exceptions;
using Wam.Games.ExtensionMethods;
using Wam.Games.Repositories;
using Wam.Core.Configuration;
using Wam.Core.Enums;

namespace Wam.Games.Services;



public class GamesService(
    IGamesRepository gamesRepository,
    DaprClient daprClient,
    IUsersService usersService,
    WebPubSubServiceClient pubsubClient,
    IFeatureManager featureManager,
    IOptions<ServicesConfiguration> servicesConfiguration,
    ILogger<GamesService> logger)
    : IGamesService
{
    private const string StateStoreName = "statestore";


    public async Task<GameDetailsDto?> GetUpcoming(CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting upcoming game");
        var game = await gamesRepository.GetNewGame(cancellationToken);
        if (game != null)
        {
            var dto = ToDto(game);
            await UpdateCache(dto, cancellationToken);
            return dto;
        }
        logger.LogInformation("No upcoming game found, returning nothing");
        return null;
    }

    public async Task<GameDetailsDto?> GetActive(CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting upcoming game");
        var game = await gamesRepository.GetActiveGame(cancellationToken);
        if (game != null)
        {
            var dto = ToDto(game);
            await UpdateCache(dto, cancellationToken);
            return dto;
        }
        logger.LogInformation("No active game found, returning nothing");
        return null;
    }

    public async Task<GameDetailsDto> Get(Guid id, CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting game by id {id}, using the cache-aside pattern", id);
        var cacheKey = CacheName.GameDetails(id);
        var cacheValue = await daprClient.GetStateEntryAsync<GameDetailsDto>(StateStoreName, cacheKey, cancellationToken: cancellationToken);
        if (cacheValue.Value != null)
        {
            return cacheValue.Value;
        }
        var dbValue = await GetFromRepositoryById(id, cancellationToken);
        await UpdateCache(dbValue, cancellationToken);
        return dbValue;
    }

    public async Task<GameDetailsDto> GetByCode(string code, CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting game by code {code}, using the cache-aside pattern", code);
        var cacheKey = CacheName.GameDetails(code);
        var cacheValue =
            await daprClient.GetStateEntryAsync<GameDetailsDto>(StateStoreName, cacheKey,
                cancellationToken: cancellationToken);
        if (cacheValue.Value != null)
        {
            return cacheValue.Value;
        }

        var dbValue = await GetFromRepositoryByCode(code, cancellationToken);
        await UpdateCache(dbValue, cancellationToken);
        return dbValue;
    }

    public async Task<GameDetailsDto> Create(CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating new game");
        var newGameAlreadyAvailable = await gamesRepository.HasNewGame(cancellationToken);
        if (newGameAlreadyAvailable)
        {
            throw new WamGameException(WamGameErrorCode.NewGameAlreadyExists,
                "There can only be one game in the new state at a time.");
        }

        var game = new Game();
        var dto = await SaveAndReturnDetails(game, cancellationToken);
        await NewGameCreated(dto);
        return dto;
    }

    public async Task<GameDetailsDto> Join(string code, Guid userId, string? voucher, CancellationToken cancellationToken)
    {
        // Throws exception when game code is invalid
        code = code.ValidateGameCode();

        logger.LogInformation("Joining game {code} as user {userId}", code, userId);
        var game = await gamesRepository.GetByCode(code, cancellationToken);
        if (game.Players.Any(plyr => plyr.Id == userId))
        {
            logger.LogInformation("User {userId} is already part of game {code}, doing nothing", userId, code);
            return ToDto(game);
        }

        if (await featureManager.IsEnabledAsync(FeatureName.EnableMaxPlayersFeature) && game.Players.Count >= 25)
        {
            throw new WamGameException(WamGameErrorCode.GameIsFull,
                "The game is full, no more players can join");
        }

        var userDetails = await usersService.GetPlayerDetails(userId, cancellationToken);
        if (userDetails == null)
        {
            throw new WamGameException(WamGameErrorCode.PlayerNotFound,
                               $"The player with id {userId} was not found in the system");
        }
        var playerModel = new Player(userDetails.Id, userDetails.DisplayName, userDetails.EmailAddress, userDetails.IsExcluded);
        if (await featureManager.IsEnabledAsync(FeatureName.EnableVouchersFeature))
        {
            if (string.IsNullOrWhiteSpace(voucher) || !Guid.TryParse(voucher, out Guid voucherId))
            {
                throw new WamGameException(WamGameErrorCode.InvalidGameVoucher,
                    "Player passed no voucher or an invalid voucher code");
            }

            var claimVoucherResponse = await ClaimVoucher(playerModel.Id, voucherId, cancellationToken);
            if (!claimVoucherResponse)
            {
                throw new WamGameException(WamGameErrorCode.InvalidGameVoucher,
                    "!Invalid voucher! Voucher code not found or already used.");
            }
            playerModel.SetVoucher(voucherId.ToString());
        }
        game.AddPlayer(playerModel);
        var dto = await SaveAndReturnDetails(game, cancellationToken);
        await PlayerAddedEvent(code, playerModel);
        return dto;
    }



    private async Task<bool> ClaimVoucher(Guid playerId, Guid voucherId, CancellationToken cancellationToken)
    {
        logger.LogInformation("Claiming voucher {voucherId} for player {playerId}", voucherId, playerId);
        var daprClientResponse = daprClient.InvokeMethodAsync(HttpMethod.Get, servicesConfiguration.Value.VouchersService, $"api/vouchers/{voucherId}/claim/{playerId}",
            cancellationToken);

        await daprClientResponse.WaitAsync(cancellationToken);
        return daprClientResponse.IsCompletedSuccessfully;
    }


    public async Task<GameDetailsDto> Leave(Guid gameId, Guid playerId, CancellationToken cancellationToken)
    {
        var game = await gamesRepository.Get(gameId, cancellationToken);
        var player = game.Players.FirstOrDefault(p => p.Id == playerId);
        if (player != null)
        {
            game.RemovePlayer(player);
        }

        if (await gamesRepository.Save(game, cancellationToken) == false)
        {
            throw new Exception("Failed to save game");
        }

        var dto = await SaveAndReturnDetails(game, cancellationToken);
        await PlayerRemovedEvent(game.Code, playerId);
        return dto;
    }

    public async Task<bool> DeletePlayer(Guid gameId, Guid playerId, CancellationToken cancellationToken)
    {
        var game = await gamesRepository.Get(gameId, cancellationToken);
        var player = game.Players.FirstOrDefault(p => p.Id == playerId);
        if (player != null)
        {
            game.BanPlayer(player);
        }

        if (await gamesRepository.Save(game, cancellationToken) == false)
        {
            throw new Exception("Failed to save game");
        }

        await SaveAndReturnDetails(game, cancellationToken);
        try
        {
            await PlayerRemovedEvent(game.Code, playerId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Player was removed from the game, but failed to ban the user as a user {playerId}", playerId);
        }

        return true;
    }

    public async Task<GameDetailsDto> Activate(Guid gameId, CancellationToken cancellationToken)
    {
        var alreadyHasActiveGame = await gamesRepository.HasActiveGame(cancellationToken);
        if (alreadyHasActiveGame)
        {
            throw new WamGameException(WamGameErrorCode.ActiveGameAlreadyExists,
                "There can only be one game in the active state at a time.");
        }

        var game = await gamesRepository.Get(gameId, cancellationToken);
        game.Activate();
        var dto = await SaveAndReturnDetails(game, cancellationToken);
        await GameStateChangedEvent(game);
        await GameBecameActive(dto);

        return dto;
    }
    public async Task<GameDetailsDto> Start(Guid gameId, CancellationToken cancellationToken)
    {
        var game = await gamesRepository.Get(gameId, cancellationToken);
        game.Start();
        var dto = await SaveAndReturnDetails(game, cancellationToken);
        await GameStateChangedEvent(game);
        return dto;


    }
    public async Task<GameDetailsDto> Finish(Guid gameId, CancellationToken cancellationToken)
    {
        var game = await gamesRepository.Get(gameId, cancellationToken);
        game.Finish();
        var dto = await SaveAndReturnDetails(game, cancellationToken);
        await GameStateChangedEvent(game);
        return dto;

    }
    public async Task<GameDetailsDto> Cancel(Guid gameId, CancellationToken cancellationToken)
    {
        var game = await gamesRepository.Get(gameId, cancellationToken);
        game.Cancel();
        var dto = await SaveAndReturnDetails(game, cancellationToken);
        await GameStateChangedEvent(game);
        return dto;

    }

    public async Task<GameConfigurationResponse> GetConfiguration(CancellationToken httpContextRequestAborted)
    {
        var enableVouchers = await featureManager.IsEnabledAsync(FeatureName.EnableVouchersFeature);
        var enableMaxPlayers = await featureManager.IsEnabledAsync(FeatureName.EnableMaxPlayersFeature);
        return new GameConfigurationResponse(enableVouchers, enableMaxPlayers);
    }

    private Task PlayerAddedEvent(string code, Player player)
    {
        var message = new RealtimeEvent<GamePlayerJoinedDto>
        {
            Message = "game-player-added",
            Data = new GamePlayerJoinedDto(code, player.Id, player.DisplayName, player.EmailAddress)
        };
        return RaiseEvent(message, code);
    }
    private Task PlayerRemovedEvent(string code, Guid playerId)
    {
        var message = new RealtimeEvent<GamePlayerLeftDto>
        {
            Message = "game-player-removed",
            Data = new GamePlayerLeftDto(code, playerId)
        };
        return RaiseEvent(message, code);
    }
    private Task GameStateChangedEvent(Game game)
    {
        var message = new RealtimeEvent<GameStateChangedDto>
        {
            Message = "game-state-changed",
            Data = new GameStateChangedDto(
                game.Id,
                game.Code,
                game.State,
                game.CreatedOn,
                game.StartedOn,
                game.FinishedOn)
        };
        return RaiseEvent(message, game.Code);
    }

    private Task GameBecameActive(GameDetailsDto game)
    {
        var message = new RealtimeEvent<GameDetailsDto>
        {
            Message = "game-became-active",
            Data = game
        };
        return RaiseEvent(message, "dashboard");
    }
    private Task NewGameCreated(GameDetailsDto game)
    {
        var message = new RealtimeEvent<GameDetailsDto>
        {
            Message = "new-game-created",
            Data = game
        };
        return RaiseEvent(message, "dashboard");
    }


    private async Task RaiseEvent<T>(RealtimeEvent<T> realtimeEvent, string group)
    {
        try
        {
            await pubsubClient.SendToGroupAsync(group, realtimeEvent.ToJson(), ContentType.ApplicationJson);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to raise event {event} to group {group}", realtimeEvent.Message, group);
        }
    }

    private async Task<GameDetailsDto> SaveAndReturnDetails(Game game, CancellationToken cancellationToken)
    {
        if (await gamesRepository.Save(game, cancellationToken) == false)
        {
            throw new Exception("Failed to save game");
        }

        var dto = ToDto(game);
        await UpdateCache(dto, cancellationToken);
        return dto;
    }

    private async Task<GameDetailsDto> GetFromRepositoryById(Guid id, CancellationToken cancellationToken)
    {
        var game = await gamesRepository.Get(id, cancellationToken);
        var dto = ToDto(game);
        return dto;
    }
    private async Task<GameDetailsDto> GetFromRepositoryByCode(string code, CancellationToken cancellationToken)
    {
        var game = await gamesRepository.GetByCode(code, cancellationToken);
        var dto = ToDto(game);
        return dto;
    }
    private static GameDetailsDto ToDto(Game game)
    {
        var dto = new GameDetailsDto
        (
            game.Id,
            game.Code,
            game.State,
            game.Players.Where(plyr=> !plyr.IsBanned).Select(p => new GamePlayerDto(p.Id, p.DisplayName, p.EmailAddress, p.IsBanned)).ToList(),
            game.CreatedOn,
            game.StartedOn,
            game.FinishedOn
        );
        return dto;
    }

    private async Task UpdateCache(GameDetailsDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var cacheKeyById = CacheName.GameDetails(dto.Id);
            var cacheKeyByCode = CacheName.GameDetails(dto.Code);
            await daprClient.SaveStateAsync(
                StateStoreName, 
                cacheKeyById, 
                dto,
                metadata: new Dictionary<string, string>
                {
                    {
                        "ttlInSeconds", "900"
                    }
                },

                cancellationToken: cancellationToken);
            await daprClient.SaveStateAsync(StateStoreName, cacheKeyByCode, dto, cancellationToken: cancellationToken);
        }
        catch (Exception e)
        {
            logger.LogError(e, "New game created successfully, but failed to update cache");
        }
    }

}