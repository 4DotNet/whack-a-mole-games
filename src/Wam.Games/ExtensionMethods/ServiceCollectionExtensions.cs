using Microsoft.Extensions.DependencyInjection;
using Wam.Games.Repositories;
using Wam.Games.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWamGamesModule(this IServiceCollection services)
    {
        services.AddTransient<IGamesService, GamesService>();
        services.AddTransient<IGamesRepository, GamesRepository>();
        return services;
    }
}