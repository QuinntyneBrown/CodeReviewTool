// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using GitAnalysis.Core.Interfaces;
using GitAnalysis.Infrastructure;
using GitAnalysis.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GitAnalysis.Infrastructure.Tests;

/// <summary>
/// Tests for ConfigureServices extension method.
/// </summary>
public class ConfigureServicesTests
{
    [Fact]
    public void AddInfrastructureServices_Should_Register_NativeGitService_By_Default()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        // Act
        services.AddInfrastructureServices(configuration);
        var serviceProvider = services.BuildServiceProvider();
        var gitService = serviceProvider.GetService<IGitService>();

        // Assert
        Assert.NotNull(gitService);
        Assert.IsType<NativeGitService>(gitService);
    }

    [Fact]
    public void AddInfrastructureServices_Should_Register_NativeGitService_When_Configured()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "GitService:UseNativeGit", "true" }
            })
            .Build();

        // Act
        services.AddInfrastructureServices(configuration);
        var serviceProvider = services.BuildServiceProvider();
        var gitService = serviceProvider.GetService<IGitService>();

        // Assert
        Assert.NotNull(gitService);
        Assert.IsType<NativeGitService>(gitService);
    }

    [Fact]
    public void AddInfrastructureServices_Should_Register_LibGit2Sharp_GitService_When_Configured()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "GitService:UseNativeGit", "false" }
            })
            .Build();

        // Act
        services.AddInfrastructureServices(configuration);
        var serviceProvider = services.BuildServiceProvider();
        var gitService = serviceProvider.GetService<IGitService>();

        // Assert
        Assert.NotNull(gitService);
        Assert.IsType<GitService>(gitService);
    }

    [Fact]
    public void AddInfrastructureServices_Should_Register_All_Required_Services()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        // Act
        services.AddInfrastructureServices(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        Assert.NotNull(serviceProvider.GetService<IGitService>());
        Assert.NotNull(serviceProvider.GetService<IGitIgnoreEngine>());
        Assert.NotNull(serviceProvider.GetService<IComparisonRequestRepository>());
        Assert.NotNull(serviceProvider.GetService<IDiffResultRepository>());
    }
}
