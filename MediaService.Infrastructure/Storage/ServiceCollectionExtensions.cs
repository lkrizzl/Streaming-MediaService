using Amazon.S3;
using MediaService.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MediaService.Infrastructure.Storage;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMinioStorage(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MinioOptions>(configuration.GetSection("Minio"));

        services.AddSingleton<IAmazonS3>(sp =>
        {
            var options = configuration.GetSection("Minio").Get<MinioOptions>()!;
            var config = new AmazonS3Config
            {
                ServiceURL = $"http://{options.Endpoint}",
                ForcePathStyle = true
            };
            return new AmazonS3Client(options.AccessKey, options.SecretKey, config);
        });

        services.AddKeyedSingleton<IAmazonS3>("public", (sp, _) =>
        {
            var options = configuration.GetSection("Minio").Get<MinioOptions>()!;
            var config = new AmazonS3Config
            {
                ServiceURL = $"http://{options.PublicEndpoint}",
                ForcePathStyle = true
            };
            return new AmazonS3Client(options.AccessKey, options.SecretKey, config);
        });

        services.AddScoped<IStorageService, MinioStorageService>();

        return services;

    }
}