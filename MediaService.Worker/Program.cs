using MediaService.Application.Interfaces;
using MediaService.Infrastructure.Messaging;
using MediaService.Infrastructure.Storage;
using MediaService.Infrastructure.Transcoding;
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

var host = builder.Build();
var testOptions = host.Services.GetRequiredService<IOptions<RabbitMqOptions>>().Value;
Console.WriteLine($"RabbitMQ Host: '{testOptions.HostName}', User: '{testOptions.UserName}'");

host.Run();