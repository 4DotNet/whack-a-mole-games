using Wam.Games.DataTransferObjects;

namespace Wam.Games.Services;

public interface IGamesService
{
    Task<GameDetailsDto> Get(Guid id, CancellationToken cancellationToken);
    Task<GameDetailsDto?> GetUpcoming(CancellationToken cancellationToken);
    Task<GameDetailsDto> GetByCode(string code, CancellationToken cancellationToken);
    Task<GameDetailsDto> Create(CancellationToken cancellationToken);
    
    Task<GameDetailsDto> Join(string code, Guid userId, string? voucherCode, CancellationToken cancellationToken);
    Task<GameDetailsDto> Leave(Guid gameId, Guid userId, CancellationToken cancellationToken);

    Task<GameDetailsDto> Activate(Guid gameId, CancellationToken cancellationToken);
    Task<GameDetailsDto> Start(Guid gameId, CancellationToken cancellationToken);
    Task<GameDetailsDto> Finish(Guid gameId, CancellationToken cancellationToken);
    Task<GameDetailsDto> Cancel(Guid gameId, CancellationToken cancellationToken);

}