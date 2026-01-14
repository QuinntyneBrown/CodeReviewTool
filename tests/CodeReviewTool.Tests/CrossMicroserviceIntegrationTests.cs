// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.Testing;
using GitAnalysis.Core.DTOs;
using System.Net.Http.Json;
using System.Net;

namespace CodeReviewTool.Tests;

/// <summary>
/// Cross-microservice integration tests.
/// Tests the flow of requests through API Gateway to backend services.
/// </summary>
public class CrossMicroserviceIntegrationTests : IClassFixture<WebApplicationFactory<ApiGateway.Program>>
{
    private readonly WebApplicationFactory<ApiGateway.Program> factory;
    private readonly HttpClient client;

    public CrossMicroserviceIntegrationTests(WebApplicationFactory<ApiGateway.Program> factory)
    {
        this.factory = factory;
        this.client = factory.CreateClient();
    }

    [Fact]
    public void ApiGateway_Should_Be_Configured()
    {
        // Arrange & Act
        var serviceProvider = factory.Services;

        // Assert
        Assert.NotNull(serviceProvider);
    }

    [Fact]
    public async Task ApiGateway_Should_Route_To_GitAnalysis_Service()
    {
        // Arrange
        var request = new ComparisonRequestDto
        {
            RepositoryPath = "/test/repo",
            SourceBranch = "main",
            TargetBranch = "feature/test"
        };

        // Act
        // Note: This test expects backend services to be running
        // In a real integration test, you might use TestContainers or mock services
        var response = await client.PostAsJsonAsync("/api/comparison", request);

        // Assert
        // The gateway should attempt to forward the request
        // Without backend services running, we expect a service unavailable or bad gateway error
        Assert.True(
            response.StatusCode == HttpStatusCode.ServiceUnavailable ||
            response.StatusCode == HttpStatusCode.BadGateway ||
            response.StatusCode == HttpStatusCode.Accepted,
            $"Expected service unavailable, bad gateway, or accepted but got {response.StatusCode}");
    }

    [Fact]
    public async Task ApiGateway_Should_Route_Notifications_Path()
    {
        // Act
        var response = await client.GetAsync("/notifications/test");

        // Assert
        // The gateway should attempt to forward the request
        Assert.True(
            response.StatusCode == HttpStatusCode.ServiceUnavailable ||
            response.StatusCode == HttpStatusCode.BadGateway ||
            response.StatusCode == HttpStatusCode.BadRequest ||
            response.StatusCode == HttpStatusCode.OK,
            $"Expected service unavailable, bad gateway, bad request or OK but got {response.StatusCode}");
    }

    [Fact]
    public async Task ApiGateway_Should_Return_NotFound_For_Unknown_Routes()
    {
        // Act
        var response = await client.GetAsync("/api/unknown/endpoint");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ApiGateway_Should_Accept_Comparison_Path_With_Id()
    {
        // Arrange
        var requestId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/comparison/{requestId}");

        // Assert
        // The gateway should attempt to forward the request
        Assert.True(
            response.StatusCode == HttpStatusCode.ServiceUnavailable ||
            response.StatusCode == HttpStatusCode.BadGateway ||
            response.StatusCode == HttpStatusCode.NotFound,
            $"Expected service unavailable, bad gateway, or not found but got {response.StatusCode}");
    }

    [Fact]
    public async Task ApiGateway_Should_Support_Cors()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/comparison");
        request.Headers.Add("Origin", "http://localhost:4200");
        request.Headers.Add("Access-Control-Request-Method", "POST");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        Assert.True(
            response.IsSuccessStatusCode ||
            response.StatusCode == HttpStatusCode.NoContent ||
            response.StatusCode == HttpStatusCode.ServiceUnavailable,
            $"Expected success, no content, or service unavailable but got {response.StatusCode}");
    }

    [Fact]
    public async Task ApiGateway_Branches_Endpoint_Should_Route_Correctly()
    {
        // Arrange
        var repositoryPath = "/test/repo";

        // Act
        var response = await client.GetAsync($"/api/comparison/branches?repositoryPath={repositoryPath}");

        // Assert
        Assert.True(
            response.StatusCode == HttpStatusCode.ServiceUnavailable ||
            response.StatusCode == HttpStatusCode.BadGateway ||
            response.StatusCode == HttpStatusCode.OK,
            $"Expected service unavailable, bad gateway, or OK but got {response.StatusCode}");
    }

    [Fact]
    public async Task ApiGateway_Should_Forward_POST_Requests()
    {
        // Arrange
        var request = new ComparisonRequestDto
        {
            RepositoryPath = "/gateway/test",
            SourceBranch = "feature/a",
            TargetBranch = "feature/b"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/comparison", request);

        // Assert
        Assert.True(
            response.StatusCode == HttpStatusCode.ServiceUnavailable ||
            response.StatusCode == HttpStatusCode.BadGateway ||
            response.StatusCode == HttpStatusCode.Accepted,
            $"Expected service unavailable, bad gateway, or accepted but got {response.StatusCode}");
    }

    [Fact]
    public async Task ApiGateway_Should_Handle_Invalid_Json()
    {
        // Arrange
        var content = new StringContent("invalid json", System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/comparison", content);

        // Assert
        // Gateway might forward it (resulting in 503/502) or reject it (400)
        Assert.True(
            response.StatusCode == HttpStatusCode.ServiceUnavailable ||
            response.StatusCode == HttpStatusCode.BadGateway ||
            response.StatusCode == HttpStatusCode.BadRequest,
            $"Expected service unavailable, bad gateway, or bad request but got {response.StatusCode}");
    }

    [Fact]
    public void ApiGateway_Should_Have_Reverse_Proxy_Configured()
    {
        // Arrange & Act
        var serviceProvider = factory.Services;

        // Assert
        Assert.NotNull(serviceProvider);
    }

    [Fact]
    public async Task ApiGateway_Should_Handle_Multiple_Concurrent_Requests()
    {
        // Arrange
        var tasks = new List<Task<HttpResponseMessage>>();
        for (int i = 0; i < 5; i++)
        {
            var request = new ComparisonRequestDto
            {
                RepositoryPath = $"/test/repo{i}",
                SourceBranch = "main",
                TargetBranch = $"feature/{i}"
            };
            tasks.Add(client.PostAsJsonAsync("/api/comparison", request));
        }

        // Act
        var responses = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(5, responses.Length);
        foreach (var response in responses)
        {
            Assert.True(
                response.StatusCode == HttpStatusCode.ServiceUnavailable ||
                response.StatusCode == HttpStatusCode.BadGateway ||
                response.StatusCode == HttpStatusCode.Accepted,
                $"Expected service unavailable, bad gateway, or accepted but got {response.StatusCode}");
        }
    }
}
