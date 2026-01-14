// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

        return services;
    }
}