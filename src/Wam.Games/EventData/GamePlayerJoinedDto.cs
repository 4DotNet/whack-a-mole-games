namespace Wam.Games.EventData;

public record GamePlayerJoinedDto(string GameCode, Guid Id, string DisplayName, string EmailAddress);