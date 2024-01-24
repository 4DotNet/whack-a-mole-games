namespace Wam.Games.EventData;

public record GamePlayerJoinedDto(string Code, Guid Id, string DisplayName, string EmailAddress);