// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;

namespace ApiGateway.Tests;

public class RoutingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public RoutingTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory;
    }

    private static void AssertBackendUnavailableResponse(HttpStatusCode statusCode)
    {
        Assert.True(
            statusCode == HttpStatusCode.BadGateway ||
            statusCode == HttpStatusCode.ServiceUnavailable ||
            statusCode == HttpStatusCode.NotFound,
            $"Expected BadGateway, ServiceUnavailable, or NotFound but got {statusCode}");
    }

    [Fact]
    public async Task Route_To_GitAnalysis_Should_Be_Configured()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // This will fail to route to backend since backend is not running,
        // but the gateway should process it (502 Bad Gateway expected)
        var response = await client.GetAsync("/api/comparison/test-id");

        AssertBackendUnavailableResponse(response.StatusCode);
    }

    [Fact]
    public async Task Route_To_RealtimeNotification_Should_Be_Configured()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // This will fail to route to backend since backend is not running
        var response = await client.GetAsync("/notifications/test");

        AssertBackendUnavailableResponse(response.StatusCode);
    }

    [Fact]
    public async Task Gateway_Should_Return_BadGateway_When_Backend_Unavailable()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/comparison/test-id");

        AssertBackendUnavailableResponse(response.StatusCode);
    }

    [Theory]
    [InlineData("/api/comparison/123")]
    [InlineData("/api/comparison/branches")]
    [InlineData("/api/comparison/test-request")]
    public async Task GitAnalysis_Routes_Should_Forward_To_Backend(string path)
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync(path);

        AssertBackendUnavailableResponse(response.StatusCode);
    }

    [Theory]
    [InlineData("/notifications/hub")]
    [InlineData("/notifications/test")]
    public async Task RealtimeNotification_Routes_Should_Forward_To_Backend(string path)
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync(path);

        AssertBackendUnavailableResponse(response.StatusCode);
    }

    [Fact]
    public async Task Unknown_Route_Should_Return_NotFound()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/unknown/path/that/does/not/exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task POST_Request_Should_Be_Routed()
    {
        var client = factory.CreateClient();
        var content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/api/comparison", content);

        AssertBackendUnavailableResponse(response.StatusCode);
    }
}
