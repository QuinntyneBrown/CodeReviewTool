// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace ApiGateway.Tests;

public class StartupTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public StartupTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory;
    }

    [Fact]
    public void Application_Should_Start_Successfully()
    {
        var client = factory.CreateClient();
        Assert.NotNull(client);
    }

    [Fact]
    public void Configuration_Should_Be_Loaded()
    {
        var configuration = factory.Services.GetService<IConfiguration>();
        Assert.NotNull(configuration);
    }

    [Fact]
    public void ReverseProxy_Services_Should_Be_Available()
    {
        var services = factory.Services;
        Assert.NotNull(services);

        var proxyConfigProvider = services.GetService<Yarp.ReverseProxy.Configuration.IProxyConfigProvider>();
        Assert.NotNull(proxyConfigProvider);
    }

    [Fact]
    public async Task Application_Should_Respond_To_Requests()
    {
        var client = factory.CreateClient();
        
        // Any request should get a response (even if 404 or 502)
        var response = await client.GetAsync("/");
        Assert.NotNull(response);
    }

    [Fact]
    public void AllowedHosts_Should_Be_Configured()
    {
        var configuration = factory.Services.GetRequiredService<IConfiguration>();
        var allowedHosts = configuration["AllowedHosts"];
        
        Assert.NotNull(allowedHosts);
    }

    [Fact]
    public void Logging_Should_Be_Configured()
    {
        var configuration = factory.Services.GetRequiredService<IConfiguration>();
        var loggingSection = configuration.GetSection("Logging");
        
        Assert.NotNull(loggingSection);
        Assert.True(loggingSection.Exists());
    }

    [Fact]
    public void Application_Should_Have_Default_LogLevel()
    {
        var configuration = factory.Services.GetRequiredService<IConfiguration>();
        var defaultLogLevel = configuration["Logging:LogLevel:Default"];
        
        Assert.NotNull(defaultLogLevel);
    }
}
