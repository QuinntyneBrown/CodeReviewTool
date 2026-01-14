// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Yarp.ReverseProxy.Configuration;

namespace ApiGateway.Tests;

public class ReverseProxyTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public ReverseProxyTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory;
    }

    [Fact]
    public void ReverseProxy_Services_Should_Be_Registered()
    {
        var proxyConfigProvider = factory.Services.GetService<IProxyConfigProvider>();
        Assert.NotNull(proxyConfigProvider);
    }

    [Fact]
    public async Task ReverseProxy_Should_Have_Two_Routes_Configured()
    {
        var proxyConfigProvider = factory.Services.GetRequiredService<IProxyConfigProvider>();
        var config = proxyConfigProvider.GetConfig();

        var routeCount = config.Routes.Count();
        Assert.True(routeCount >= 2, $"Expected at least 2 routes but found {routeCount}");
    }

    [Fact]
    public async Task ReverseProxy_Should_Have_Two_Clusters_Configured()
    {
        var proxyConfigProvider = factory.Services.GetRequiredService<IProxyConfigProvider>();
        var config = proxyConfigProvider.GetConfig();

        var clusterCount = config.Clusters.Count();
        Assert.True(clusterCount >= 2, $"Expected at least 2 clusters but found {clusterCount}");
    }

    [Fact]
    public async Task ReverseProxy_Should_Have_GitAnalysis_Route()
    {
        var proxyConfigProvider = factory.Services.GetRequiredService<IProxyConfigProvider>();
        var config = proxyConfigProvider.GetConfig();

        var gitAnalysisRoute = config.Routes.FirstOrDefault(r => r.RouteId == "git-analysis-route");
        Assert.NotNull(gitAnalysisRoute);
        Assert.Equal("git-analysis-cluster", gitAnalysisRoute.ClusterId);
    }

    [Fact]
    public async Task ReverseProxy_Should_Have_RealtimeNotification_Route()
    {
        var proxyConfigProvider = factory.Services.GetRequiredService<IProxyConfigProvider>();
        var config = proxyConfigProvider.GetConfig();

        var notificationRoute = config.Routes.FirstOrDefault(r => r.RouteId == "realtime-notification-route");
        Assert.NotNull(notificationRoute);
        Assert.Equal("realtime-notification-cluster", notificationRoute.ClusterId);
    }

    [Fact]
    public async Task ReverseProxy_GitAnalysis_Route_Should_Match_Comparison_Path()
    {
        var proxyConfigProvider = factory.Services.GetRequiredService<IProxyConfigProvider>();
        var config = proxyConfigProvider.GetConfig();

        var gitAnalysisRoute = config.Routes.FirstOrDefault(r => r.RouteId == "git-analysis-route");
        Assert.NotNull(gitAnalysisRoute);
        Assert.NotNull(gitAnalysisRoute.Match);
        Assert.NotNull(gitAnalysisRoute.Match.Path);
        Assert.Contains("/api/comparison/", gitAnalysisRoute.Match.Path);
    }

    [Fact]
    public async Task ReverseProxy_RealtimeNotification_Route_Should_Match_Notifications_Path()
    {
        var proxyConfigProvider = factory.Services.GetRequiredService<IProxyConfigProvider>();
        var config = proxyConfigProvider.GetConfig();

        var notificationRoute = config.Routes.FirstOrDefault(r => r.RouteId == "realtime-notification-route");
        Assert.NotNull(notificationRoute);
        Assert.NotNull(notificationRoute.Match);
        Assert.NotNull(notificationRoute.Match.Path);
        Assert.Contains("/notifications/", notificationRoute.Match.Path);
    }

    [Fact]
    public async Task ReverseProxy_Should_Have_GitAnalysis_Cluster_With_Destination()
    {
        var proxyConfigProvider = factory.Services.GetRequiredService<IProxyConfigProvider>();
        var config = proxyConfigProvider.GetConfig();

        var gitAnalysisCluster = config.Clusters.FirstOrDefault(c => c.ClusterId == "git-analysis-cluster");
        Assert.NotNull(gitAnalysisCluster);
        Assert.NotNull(gitAnalysisCluster.Destinations);
        Assert.NotEmpty(gitAnalysisCluster.Destinations);

        var destination = gitAnalysisCluster.Destinations.First().Value;
        Assert.NotNull(destination.Address);
    }

    [Fact]
    public async Task ReverseProxy_Should_Have_RealtimeNotification_Cluster_With_Destination()
    {
        var proxyConfigProvider = factory.Services.GetRequiredService<IProxyConfigProvider>();
        var config = proxyConfigProvider.GetConfig();

        var notificationCluster = config.Clusters.FirstOrDefault(c => c.ClusterId == "realtime-notification-cluster");
        Assert.NotNull(notificationCluster);
        Assert.NotNull(notificationCluster.Destinations);
        Assert.NotEmpty(notificationCluster.Destinations);

        var destination = notificationCluster.Destinations.First().Value;
        Assert.NotNull(destination.Address);
    }

    [Fact]
    public void ReverseProxy_Configuration_Should_Be_Valid()
    {
        var proxyConfigProvider = factory.Services.GetRequiredService<IProxyConfigProvider>();
        var config = proxyConfigProvider.GetConfig();

        Assert.NotNull(config);
        Assert.NotNull(config.Routes);
        Assert.NotNull(config.Clusters);
    }

    [Fact]
    public async Task ReverseProxy_Routes_Should_Have_Valid_ClusterIds()
    {
        var proxyConfigProvider = factory.Services.GetRequiredService<IProxyConfigProvider>();
        var config = proxyConfigProvider.GetConfig();

        var clusterIds = config.Clusters.Select(c => c.ClusterId).ToHashSet();

        foreach (var route in config.Routes)
        {
            Assert.True(clusterIds.Contains(route.ClusterId), 
                $"Route {route.RouteId} references cluster {route.ClusterId} which doesn't exist");
        }
    }
}
