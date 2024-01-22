using Wam.Games.DataTransferObjects;

namespace Wam.Games.Services;

public interface IUsersService
{
    Task<PlayerDetailsDto?> GetPlayerDetails(Guid userId, CancellationToken cancellationToken);
    Task<PlayerDetailsDto?> BanUser(Guid userId, CancellationToken cancellationToken);
}