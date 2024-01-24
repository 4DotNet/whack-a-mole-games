using HexMaster.DomainDrivenDesign;
using HexMaster.DomainDrivenDesign.ChangeTracking;
using Wam.Core.Enums;
using Wam.Core.ExtensionMethods;
using Wam.Games.ErrorCodes;
using Wam.Games.Exceptions;

namespace Wam.Games.DomainModels;

/// <summary>
/// This is the aggregate root for a game
/// </summary>
public class Game : DomainModel<Guid>
{
    private readonly List<Player> _players;
    public string Code { get; }
    public GameState State { get; private set; }
    public DateTimeOffset CreatedOn { get; private set; }
    public DateTimeOffset? StartedOn { get; private set; }
    public DateTimeOffset? FinishedOn { get; private set; }
    public IReadOnlyList<Player> Players => _players.AsReadOnly();

    public void AddPlayer(Player player)
    {
        if (_players.Any(p => p.Id == player.Id))
        {
            throw new InvalidOperationException("Player already added");
        }

        if (_players.Count >= 25)
        {
            throw new WamGameException(WamGameErrorCode.GameIsFull,
                "The maximum amount of players is reached, no new players can join at this time");
        }

        if (State != GameState.New && State != GameState.Current)
        {
            throw new WamGameException(WamGameErrorCode.InvalidState,
                "Cannot add players to a game of an active state");
        }

        _players.Add(player);
        SetState(TrackingState.Modified);
    }
    public void RemovePlayer(Player player)
    {
        if (_players.Contains(player))
        {
            throw new InvalidOperationException("Player not found");
        }

        _players.Remove(player);
        SetState(TrackingState.Modified);
    }

    public void BanPlayer(Player player)
    {
        if (!_players.Contains(player))
        {
            throw new InvalidOperationException("Player not found");
        }

        player.Ban();
        SetState(TrackingState.Modified);
    }
    private void ChangeState(GameState value)
    {
        if (State.CanChangeTo(value))
        {
            State = value;
            if (value == GameState.Started)
            {
                StartedOn = DateTimeOffset.UtcNow;
                FinishedOn = DateTimeOffset.UtcNow;
            }
            if (value == GameState.Cancelled)
            {
                FinishedOn = DateTimeOffset.UtcNow;
            }
            SetState(TrackingState.Modified);
        }
        else
        {
            throw new WamGameException(WamGameErrorCode.InvalidState,
                $"The current game state {State.Code} cannot be changed to {value.Code}");
        }
    }

    public void Activate()
    {
        ChangeState(GameState.Current);
    }

    public void Start()
    {
        ChangeState(GameState.Started);
    }

    public void Finish()
    {
        ChangeState(GameState.Started);
    }

    public void Cancel()
    {
        ChangeState(GameState.Cancelled);
    }

    public Game(Guid id,
        string code,
        string state,
        DateTimeOffset createdOn,
        DateTimeOffset? startedOn = null,
        DateTimeOffset? finishedOn = null,
        List<Player>? players = null) : base(id)
    {
        Code = code;
        State = GameState.FromCode(state);
        CreatedOn = createdOn;
        StartedOn = startedOn;
        FinishedOn = finishedOn;
        _players = players ?? [];
    }

    public Game() : base(Guid.NewGuid(), TrackingState.New)
    {
        Code = StringExtensions.GenerateGameCode();
        CreatedOn = DateTimeOffset.UtcNow;
        State = GameState.New;
        _players = [];
    }
}