// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using CodeReviewTool.Shared.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using GitAnalysis.Core.Interfaces;
using GitAnalysis.Infrastructure.Services;
using GitAnalysis.Infrastructure.Repositories;
using GitAnalysis.Infrastructure.BackgroundServices;

namespace GitAnalysis.Infrastructure;

public static class ConfigureServices
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IGitService, GitService>();
        services.AddSingleton<IGitIgnoreEngine, GitIgnoreEngine>();
        services.AddSingleton<IComparisonRequestRepository, ComparisonRequestRepository>();
        services.AddSingleton<IDiffResultRepository, DiffResultRepository>();
        services.AddSingleton<ComparisonProcessorService>();
        services.AddHostedService(sp => sp.GetRequiredService<ComparisonProcessorService>());

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

        services.AddHostedService<AnalysisRequestHandler>();

        return services;
    }
}