using MediaService.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MediaService.Infrastructure.Webhooks;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWebApiNotifier(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<WebApiOptions>(configuration.GetSection("WebApi"));
        services.AddHttpClient<IEpisodeNotifier, WebApiNotifier>();

        return services;
    }
}