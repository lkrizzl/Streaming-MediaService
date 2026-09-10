using MediaService.Application.Interfaces;
using MediaService.Infrastructure.Messaging;
using MediaService.Infrastructure.Storage;
using MediaService.Infrastructure.Transcoding;
using MediaService.Infrastructure.Webhooks;
using MediaService.Persistence;
using MediaService.Worker.Consumers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddMinioStorage(builder.Configuration);
builder.Services.AddPersistence(builder.Configuration);
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMq"));
builder.Services.AddScoped<ITranscodingService, FfmpegTranscodingService>();
builder.Services.AddHostedService<TranscodeJobConsumer>();
builder.Services.AddWebApiNotifier(builder.Configuration);

var host = builder.Build();

host.Run();