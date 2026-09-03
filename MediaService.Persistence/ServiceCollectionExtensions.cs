using MediaService.Application.Interfaces;
using MediaService.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MediaService.Persistence;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<MediaDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("MediaDb")));

        services.AddScoped<IEpisodeMediaRepository, EpisodeMediaRepository>();

        return services;
    }
}