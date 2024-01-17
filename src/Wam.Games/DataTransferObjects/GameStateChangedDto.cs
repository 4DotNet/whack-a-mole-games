namespace Wam.Games.DataTransferObjects;

public record GameStateChangedDto(
    Guid Id,
    string Code,
    string State,
    DateTimeOffset CreatedOn,
    DateTimeOffset? StartedOn,
    DateTimeOffset? FinishedOn);
