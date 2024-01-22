using Wam.Games.DomainModels;

namespace Wam.Games.Repositories;

public interface IGamesRepository
{
    Task<bool> Save(Game game, CancellationToken cancellationToken);
    Task<Game> Get(Guid gameId, CancellationToken cancellationToken);
    Task<Game> GetByCode(string code, CancellationToken cancellationToken);

    /// <summary>
    /// This method returns true if there is a game in the repository that has status new.
    /// </summary>
    /// <returns></returns>
    Task<bool> HasNewGame(CancellationToken cancellationToken);

    Task<Game?> GetNewGame(CancellationToken cancellationToken);

    /// <summary>
    /// This method returns true if there is a game in the repository that has status current or started.
    /// </summary>
    /// <returns></returns>
    Task<bool> HasActiveGame(CancellationToken cancellationToken);
    Task<Game?> GetActiveGame(CancellationToken cancellationToken);

}