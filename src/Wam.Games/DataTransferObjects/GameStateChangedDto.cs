using Wam.Core.Enums;

namespace Wam.Games.DataTransferObjects;

public record GameStateChangedDto(
    Guid Id,
    string Code,
    GameState State,
    DateTimeOffset CreatedOn,
    DateTimeOffset? StartedOn,
    DateTimeOffset? FinishedOn);
