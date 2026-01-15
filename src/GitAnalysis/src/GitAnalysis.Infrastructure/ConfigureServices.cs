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
using GitAnalysis.Infrastructure.Configuration;

namespace GitAnalysis.Infrastructure;

public static class ConfigureServices
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure GitService options
        services.Configure<GitServiceOptions>(configuration.GetSection("GitService"));
        
        // Register the appropriate Git service implementation
        var gitServiceOptions = configuration.GetSection("GitService").Get<GitServiceOptions>() ?? new GitServiceOptions();
        
        if (gitServiceOptions.UseNativeGit)
        {
            services.AddSingleton<IGitService, NativeGitService>();
        }
        else
        {
            services.AddSingleton<IGitService, GitService>();
        }
        
        services.AddSingleton<IGitIgnoreEngine, GitIgnoreEngine>();
        services.AddSingleton<IComparisonRequestRepository, ComparisonRequestRepository>();
        services.AddSingleton<IDiffResultRepository, DiffResultRepository>();
        services.AddSingleton<ComparisonProcessorService>();
        services.AddHostedService(sp => sp.GetRequiredService<ComparisonProcessorService>());

        return services;
    }
}