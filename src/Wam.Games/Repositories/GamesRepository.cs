using Azure.Data.Tables;
using Microsoft.Extensions.Options;
using Wam.Core.Configuration;
using Wam.Core.Enums;
using Wam.Core.ExtensionMethods;
using Wam.Core.Identity;
using Wam.Games.DomainModels;
using Wam.Games.Entities;
using Wam.Games.ErrorCodes;
using Wam.Games.Exceptions;

namespace Wam.Games.Repositories;

public class GamesRepository : IGamesRepository
{
    private const string TableName = "games";
    private const string PartitionKey = "game";
    private readonly TableClient _tableClient;

    public async Task<bool> Save(Game game, CancellationToken cancellationToken)
    {
        var entity = new GameEntity
        {
            PartitionKey = PartitionKey,
            RowKey = game.Id.ToString(),
            Code = game.Code,
            State = game.State.Code,
            CreatedOn = game.CreatedOn,
            StartedOn = game.StartedOn,
            FinishedOn = game.FinishedOn,
            Timestamp = DateTimeOffset.UtcNow
        };
        var players = game.Players.Select(player => new GamePlayerEntity
        {
            PartitionKey = game.Id.ToString(),
            RowKey = player.Id.ToString(),
            DisplayName = player.DisplayName,
            EmailAddress = player.EmailAddress,
            IsBanned = player.IsBanned,
            Timestamp = DateTimeOffset.UtcNow
        }).ToList();

        var playersActions = new List<TableTransactionAction>();
        playersActions.AddRange(players.Select(plyr => new TableTransactionAction(TableTransactionActionType.UpsertReplace, plyr)));
        
        var response = await _tableClient.UpsertEntityAsync(
            entity,
            TableUpdateMode.Replace,
            cancellationToken);

        if (playersActions.Any())
        {
            await _tableClient.SubmitTransactionAsync(
                playersActions,
                cancellationToken);
        }

        return response.Status.IsHttpSuccessCode();
    }
    
    public async Task<Game> Get(Guid gameId, CancellationToken cancellationToken)
    {
        var entity = await GetFromTableStorage(gameId, cancellationToken);
        var players = await GetGamePlayers(gameId, cancellationToken);
        return new Game(gameId,
            entity.Code,
            entity.State,
            entity.CreatedOn,
            entity.StartedOn,
            entity.FinishedOn,
            players);
    }

    public async Task<Game> GetByCode(string code, CancellationToken cancellationToken)
    {
        var entity = await GetFromTableStorageByCode(code, cancellationToken);
        var players = await GetGamePlayers(Guid.Parse(entity.RowKey), cancellationToken);

        return new Game(Guid.Parse(entity.RowKey),
            entity.Code,
            entity.State,
            entity.CreatedOn,
            entity.StartedOn,
            entity.FinishedOn,
            players);
    }

    public async Task<bool> HasNewGame(CancellationToken cancellationToken)
    {
        var query = _tableClient
            .QueryAsync<GameEntity>(
                $"{nameof(GameEntity.PartitionKey)} eq '{PartitionKey}' and {nameof(GameEntity.State)} eq '{GameState.New.Code}'");

        await foreach (var queryPage in query.AsPages().WithCancellation(cancellationToken))
        {
            return queryPage.Values.Any();
        }

        return false;
    }

    public async Task<Game?> GetNewGame(CancellationToken cancellationToken)
    {
        var query = _tableClient
            .QueryAsync<GameEntity>(
                $"{nameof(GameEntity.PartitionKey)} eq '{PartitionKey}' and {nameof(GameEntity.State)} eq '{GameState.New.Code}'");

        await foreach (var queryPage in query.AsPages().WithCancellation(cancellationToken))
        {
            if (queryPage.Values.Any())
            {
                var entity = queryPage.Values.First();
                var players = await GetGamePlayers(Guid.Parse(entity.RowKey), cancellationToken);

                return new Game(Guid.Parse(entity.RowKey),
                    entity.Code,
                    entity.State,
                    entity.CreatedOn,
                    entity.StartedOn,
                    entity.FinishedOn,
                    players);
            }
        }

        return null;
    }

    public async Task<Game?> GetActiveGame(CancellationToken cancellationToken)
    {
        var query = _tableClient
            .QueryAsync<GameEntity>(
                $"{nameof(GameEntity.PartitionKey)} eq '{PartitionKey}' and ({nameof(GameEntity.State)} eq '{GameState.Current.Code}' or {nameof(GameEntity.State)} eq '{GameState.Started.Code}')");

        await foreach (var queryPage in query.AsPages().WithCancellation(cancellationToken))
        {
            if (queryPage.Values.Any())
            {
                var entity = queryPage.Values.First();
                var players = await GetGamePlayers(Guid.Parse(entity.RowKey), cancellationToken);

                return new Game(Guid.Parse(entity.RowKey),
                    entity.Code,
                    entity.State,
                    entity.CreatedOn,
                    entity.StartedOn,
                    entity.FinishedOn,
                    players);
            }
        }

        return null;
    }

    public async Task<bool> HasActiveGame(CancellationToken cancellationToken)
    {
        var query = _tableClient
            .QueryAsync<GamePlayerEntity>($"{nameof(GameEntity.PartitionKey)} eq '{PartitionKey}' and ({nameof(GameEntity.State)} eq '{GameState.Current.Code}' or {nameof(GameEntity.State)} eq '{GameState.Started.Code}')");

        await foreach (var queryPage in query.AsPages().WithCancellation(cancellationToken))
        {
            return queryPage.Values.Any();
        }

        return false;
    }



    private async Task<GameEntity> GetFromTableStorage(Guid gameId, CancellationToken cancellationToken)
    {
        var cloudResponse = await _tableClient.GetEntityAsync<GameEntity>(
            PartitionKey, 
            gameId.ToString(),
            cancellationToken: cancellationToken);

        if (cloudResponse.HasValue)
        {
            return cloudResponse.Value;
        }

        throw new WamGameException(WamGameErrorCode.GameNotFound, $"A game with ID {gameId} could not be found");
    }

    private async Task<GameEntity> GetFromTableStorageByCode(string code, CancellationToken cancellationToken)
    {
        var query = _tableClient
            .QueryAsync<GameEntity>(
                $"{nameof(GameEntity.PartitionKey)} eq '{PartitionKey}' and {nameof(GameEntity.Code)} eq '{code}'");

        await foreach (var queryPage in query.AsPages().WithCancellation(cancellationToken))
        {
            if (queryPage.Values.Any())
            {
                return queryPage.Values.First();
            }
        }

        throw new WamGameException(WamGameErrorCode.GameNotFound, $"A game with code {code} could not be found");
    }

    private async Task<List<Player>> GetGamePlayers(Guid gameId, CancellationToken cancellationToken)
    {
        var entities = await GetGamePlayersFromRepository(gameId, cancellationToken);

        return entities.Select(ent => new Player(
            Guid.Parse(ent.RowKey),
            ent.DisplayName,
            ent.EmailAddress,
            ent.IsBanned)).ToList();
    }


    private async Task<List<GamePlayerEntity>> GetGamePlayersFromRepository(Guid gameId, CancellationToken cancellationToken)
    {
        var players = new List<GamePlayerEntity>();
        var query = _tableClient.QueryAsync<GamePlayerEntity>($"{nameof(GamePlayerEntity.PartitionKey)} eq '{gameId}'");
        await foreach (var queryPage in query.AsPages().WithCancellation(cancellationToken))
        {
            players.AddRange(queryPage.Values);
        }

        return players;
    }

    public GamesRepository(
        IOptions<AzureServices> configuration)
    {
        var tableStorageUrl = $"https://{configuration.Value.GamesStorageAccountName}.table.core.windows.net";
        _tableClient = new TableClient(new Uri(tableStorageUrl), TableName, CloudIdentity.GetCloudIdentity());
    }
}