// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using CodeReviewTool.Shared.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReportingService.Core.Interfaces;
using ReportingService.Infrastructure.BackgroundServices;
using ReportingService.Infrastructure.Services;

namespace ReportingService.Infrastructure;

public static class ConfigureServices
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IReportRepository, ReportRepository>();
        services.AddSingleton<IReportGenerator, ReportGenerator>();
        services.AddSingleton<IStorageService, LocalStorageService>();

        var messagingHost = configuration.GetValue<string>("Messaging:Host") ?? "127.0.0.1";
        var messagingPort = configuration.GetValue<int>("Messaging:Port", 9000);

        services.AddSingleton<IMessagePublisher>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<UdpMessagePublisher>>();
            return new UdpMessagePublisher(messagingHost, messagingPort, logger);
        });

        services.AddSingleton<IMessageSubscriber>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<UdpMessageSubscriber>>();
            return new UdpMessageSubscriber(messagingPort, logger);
        });

        services.AddHostedService<ReportGenerationService>();

        return services;
    }
}
