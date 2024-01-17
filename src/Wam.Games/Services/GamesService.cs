using Azure.Core;
using Azure.Messaging.WebPubSub;
using HexMaster.RedisCache.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Wam.Core.Events;
using Wam.Games.DataTransferObjects;
using Wam.Games.DomainModels;
using Wam.Games.ErrorCodes;
using Wam.Games.Exceptions;
using Wam.Games.Repositories;

namespace Wam.Games.Services;


/// <summary>
/// This is the service that handles all game related operations
/// </summary>
/// <param name="gamesRepository">The repository that stores and retrieves game information</param>
/// <param name="usersRepository">The repository that stores and retrieves user information</param>
/// <param name="cacheClientFactory">A factory service for cache client</param>
/// <param name="configuration">The system configuration</param>
/// <param name="pubsubClient">A PubSub client allowing for realtime communication</param>
/// <param name="logger">A logger to log stuff and track problems</param>
public class GamesService(
    IGamesRepository gamesRepository,
    ICacheClientFactory cacheClientFactory,
    IConfiguration configuration,
    WebPubSubServiceClient pubsubClient,
    ILogger<GamesService> logger) : IGamesService
{

    public async Task<GameDetailsDto?> GetUpcoming(CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting upcoming game");
        var game = await gamesRepository.GetNewGame(cancellationToken);
        if (game != null)
        {
            var dto = ToDto(game);
            await UpdateCache(dto);
            return dto;
        }
        logger.LogInformation("No upcoming game found, returning nothing");
        return null;
    }
    public Task<GameDetailsDto> Get(Guid id, CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting game by id {id}, using the cache-aside pattern", id);
        var cacheKey = $"wam:game:id:{id}";
        var cacheClient = cacheClientFactory.CreateClient();
        return cacheClient.GetOrInitializeAsync(() => GetFromRepositoryById(id, cancellationToken), cacheKey);
    }
    public Task<GameDetailsDto> GetByCode(string code, CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting game by code {code}, using the cache-aside pattern", code);
        var cacheKey = $"wam:game:code:{code}";
        var cacheClient = cacheClientFactory.CreateClient();
        return cacheClient.GetOrInitializeAsync(() => GetFromRepositoryByCode(code, cancellationToken), cacheKey);
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
        return await SaveAndReturnDetails(game, cancellationToken);
    }
    public async Task<GameDetailsDto> Join(string code, Guid userId, string? voucher, CancellationToken cancellationToken)
    {
        logger.LogInformation("Joining game {code} as user {userId}", code, userId);
        var game = await gamesRepository.GetByCode(code, cancellationToken);
        if (game.Players.Any(plyr => plyr.Id == userId))
        {
            logger.LogInformation("User {userId} is already part of game {code}, doing nothing", userId, code);
            return ToDto(game);
        }

        var useVouchers = false;
        if (bool.TryParse(configuration["EnableVouchers"], out bool value))
        {
            useVouchers = value;
        }

        //var user = await usersRepository.Get(userId, cancellationToken);
        //var playerModel = new Player(user.Id, user.DisplayName, user.EmailAddress, user.IsExcluded);
        //if (useVouchers)
        //{
        //    logger.LogInformation("Vouchers are enabled, validating voucher {voucher}", voucher);
        //    if (!string.IsNullOrWhiteSpace(voucher))
        //    {
        //        // Implement validation and usage of a voucher

        //        playerModel.SetVoucher(voucher);
        //    }
        //    else
        //    {
        //        throw new WamGameException(WamGameErrorCode.InvalidGameVoucher,
        //            "This game needs a voucher to join, the voucher passed is null or an empty string");
        //    }
        //}

        //game.AddPlayer(playerModel);

        var dto = await SaveAndReturnDetails(game, cancellationToken);
        //await PlayerAddedEvent(code, playerModel);
        return dto;
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

    private async Task PlayerAddedEvent(string code, Player player)
    {
        var message = new RealtimeEvent<GamePlayerDto>
        {
            Message = "PlayerAdded",
            Data = new GamePlayerDto(player.Id, player.DisplayName, player.EmailAddress, player.IsBanned)
        };
        await pubsubClient.SendToGroupAsync(code, message.ToJson(), ContentType.ApplicationJson);
    }
    private async Task PlayerRemovedEvent(string code, Guid playerId)
    {
        var message = new RealtimeEvent<GamePlayerDto>
        {
            Message = "PlayerRemoved",
            Data = new GamePlayerDto(playerId, string.Empty, string.Empty, false)
        };
        await pubsubClient.SendToGroupAsync(code, message.ToJson(), ContentType.ApplicationJson);
    }
    private async Task GameStateChangedEvent(Game game)
    {
        var message = new RealtimeEvent<GameStateChangedDto>
        {
            Message = "GameStarted",
            Data = new GameStateChangedDto(game.Id, game.Code, game.State.Code, game.CreatedOn, game.StartedOn,
                game.FinishedOn)
        };
        await pubsubClient.SendToGroupAsync(game.Code, message.ToJson(), ContentType.ApplicationJson);
    }

    private async Task<GameDetailsDto> SaveAndReturnDetails(Game game, CancellationToken cancellationToken)
    {
        if (await gamesRepository.Save(game, cancellationToken) == false)
        {
            throw new Exception("Failed to save game");
        }

        var dto = ToDto(game);
        await UpdateCache(dto);
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
            game.Players.Select(p => new GamePlayerDto(p.Id, p.DisplayName, p.EmailAddress, p.IsBanned)).ToList()
        );
        return dto;
    }

    private async Task UpdateCache(GameDetailsDto dto)
    {
        try
        {
            var cacheKeyById = $"wam:game:id:{dto.Id}";
            var cacheKeyByCode = $"wam:game:code:{dto.Code}";
            var cacheClient = cacheClientFactory.CreateClient();
            await cacheClient.SetAsAsync(cacheKeyById, dto);
            await cacheClient.SetAsAsync(cacheKeyByCode, dto);
        }
        catch (Exception e)
        {
            logger.LogError(e, "New game created successfully, but failed to update cache");
        }
    }

}