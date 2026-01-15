// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Couchbase.Lite;
using CodeReviewTool.Shared.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RepositoryService.Core.Interfaces;
using RepositoryService.Infrastructure.BackgroundServices;
using RepositoryService.Infrastructure.GitProviders;
using RepositoryService.Infrastructure.Repositories;
using RepositoryService.Infrastructure.Services;

namespace RepositoryService.Infrastructure;

public static class ConfigureServices
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var dbPath = configuration.GetValue<string>("Database:Path") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RepositoryService");
        Directory.CreateDirectory(dbPath);

        var databaseConfig = new DatabaseConfiguration
        {
            Directory = dbPath
        };

        var database = new Database("repositoryservice", databaseConfig);
        services.AddSingleton(database);

        services.AddSingleton<IRepositoryRepository, RepositoryRepository>();
        services.AddSingleton<IBranchRepository, BranchRepository>();
        services.AddSingleton<IGitService, GitService>();
        services.AddSingleton<IGitProviderAdapter, GitHubAdapter>();

        var messagingHost = configuration.GetValue<string>("Messaging:Host") ?? "127.0.0.1";
        var messagingPort = configuration.GetValue<int>("Messaging:Port", 9000);

        services.AddSingleton<IMessagePublisher>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<UdpMessagePublisher>>();
            return new UdpMessagePublisher(messagingHost, messagingPort, logger);
        });

        services.AddHostedService<RepositoryMonitorService>();

        return services;
    }
}
