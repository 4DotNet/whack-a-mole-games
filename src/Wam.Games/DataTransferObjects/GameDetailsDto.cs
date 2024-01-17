using Wam.Core.Enums;

namespace Wam.Games.DataTransferObjects;

public record GameDetailsDto(Guid Id, string Code, GameState State, List<GamePlayerDto> Players);