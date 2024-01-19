namespace Wam.Games.DataTransferObjects;

public record PlayerDetailsDto(Guid Id, string DisplayName, string EmailAddress, bool IsExcluded, string? ExclusionReason);