// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Yarp.ReverseProxy.Configuration;

namespace ApiGateway.Tests;

public class ApiGatewayConfigurationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public ApiGatewayConfigurationTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory;
    }

    [Fact]
    public void ApiGateway_Should_Have_ReverseProxy_Configuration()
    {
        var configuration = factory.Services.GetRequiredService<IConfiguration>();
        var reverseProxySection = configuration.GetSection("ReverseProxy");

        Assert.NotNull(reverseProxySection);
        Assert.True(reverseProxySection.Exists());
    }

    [Fact]
    public void ApiGateway_Should_Have_GitAnalysis_Route_Configured()
    {
        var configuration = factory.Services.GetRequiredService<IConfiguration>();
        var routes = configuration.GetSection("ReverseProxy:Routes");

        var gitAnalysisRoute = routes.GetSection("git-analysis-route");
        Assert.NotNull(gitAnalysisRoute);
        Assert.Equal("git-analysis-cluster", gitAnalysisRoute["ClusterId"]);
    }

    [Fact]
    public void ApiGateway_Should_Have_RealtimeNotification_Route_Configured()
    {
        var configuration = factory.Services.GetRequiredService<IConfiguration>();
        var routes = configuration.GetSection("ReverseProxy:Routes");

        var notificationRoute = routes.GetSection("realtime-notification-route");
        Assert.NotNull(notificationRoute);
        Assert.Equal("realtime-notification-cluster", notificationRoute["ClusterId"]);
    }

    [Fact]
    public void ApiGateway_Should_Have_GitAnalysis_Cluster_Configured()
    {
        var configuration = factory.Services.GetRequiredService<IConfiguration>();
        var clusters = configuration.GetSection("ReverseProxy:Clusters");

        var gitAnalysisCluster = clusters.GetSection("git-analysis-cluster");
        Assert.NotNull(gitAnalysisCluster);

        var destination = gitAnalysisCluster.GetSection("Destinations:destination1");
        Assert.NotNull(destination["Address"]);
    }

    [Fact]
    public void ApiGateway_Should_Have_RealtimeNotification_Cluster_Configured()
    {
        var configuration = factory.Services.GetRequiredService<IConfiguration>();
        var clusters = configuration.GetSection("ReverseProxy:Clusters");

        var notificationCluster = clusters.GetSection("realtime-notification-cluster");
        Assert.NotNull(notificationCluster);

        var destination = notificationCluster.GetSection("Destinations:destination1");
        Assert.NotNull(destination["Address"]);
    }

    [Fact]
    public void ApiGateway_Should_Have_ProxyConfigProvider_Registered()
    {
        var proxyConfigProvider = factory.Services.GetService<IProxyConfigProvider>();
        Assert.NotNull(proxyConfigProvider);
    }

    [Fact]
    public async Task ProxyConfigProvider_Should_Load_Routes()
    {
        var proxyConfigProvider = factory.Services.GetRequiredService<IProxyConfigProvider>();
        var config = proxyConfigProvider.GetConfig();

        Assert.NotNull(config);
        Assert.NotNull(config.Routes);
        Assert.NotEmpty(config.Routes);
    }

    [Fact]
    public async Task ProxyConfigProvider_Should_Load_Clusters()
    {
        var proxyConfigProvider = factory.Services.GetRequiredService<IProxyConfigProvider>();
        var config = proxyConfigProvider.GetConfig();

        Assert.NotNull(config);
        Assert.NotNull(config.Clusters);
        Assert.NotEmpty(config.Clusters);
    }

    [Fact]
    public void ApiGateway_Should_Have_GitAnalysis_Route_Path_Pattern()
    {
        var configuration = factory.Services.GetRequiredService<IConfiguration>();
        var path = configuration["ReverseProxy:Routes:git-analysis-route:Match:Path"];

        Assert.NotNull(path);
        Assert.Contains("/api/comparison/", path);
    }

    [Fact]
    public void ApiGateway_Should_Have_RealtimeNotification_Route_Path_Pattern()
    {
        var configuration = factory.Services.GetRequiredService<IConfiguration>();
        var path = configuration["ReverseProxy:Routes:realtime-notification-route:Match:Path"];

        Assert.NotNull(path);
        Assert.Contains("/notifications/", path);
    }
}
