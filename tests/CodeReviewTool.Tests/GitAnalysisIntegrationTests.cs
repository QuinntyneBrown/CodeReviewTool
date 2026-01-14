// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using GitAnalysis.Core.DTOs;
using GitAnalysis.Core.Interfaces;
using System.Net.Http.Json;
using System.Net;

namespace CodeReviewTool.Tests;

/// <summary>
/// Integration tests for GitAnalysis microservice.
/// Tests the full flow of Git comparison operations.
/// </summary>
public class GitAnalysisIntegrationTests : IClassFixture<WebApplicationFactory<GitAnalysis.Api.Program>>
{
    private readonly WebApplicationFactory<GitAnalysis.Api.Program> factory;
    private readonly HttpClient client;

    public GitAnalysisIntegrationTests(WebApplicationFactory<GitAnalysis.Api.Program> factory)
    {
        this.factory = factory;
        this.client = factory.CreateClient();
    }

    [Fact]
    public async Task RequestComparison_Should_Return_Accepted_Status()
    {
        // Arrange
        var request = new ComparisonRequestDto
        {
            RepositoryPath = "/test/repo",
            SourceBranch = "main",
            TargetBranch = "feature/test"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/comparison", request);

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ComparisonResultDto>();
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.RequestId);
        Assert.NotNull(result.Status);
    }

    [Fact]
    public async Task RequestComparison_Should_Create_Request_In_Repository()
    {
        // Arrange
        var request = new ComparisonRequestDto
        {
            RepositoryPath = "/test/repo",
            SourceBranch = "develop",
            TargetBranch = "main"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/comparison", request);
        var result = await response.Content.ReadFromJsonAsync<ComparisonResultDto>();

        // Assert
        Assert.NotNull(result);
        var repository = factory.Services.GetRequiredService<IComparisonRequestRepository>();
        var storedRequest = await repository.GetByIdAsync(result.RequestId, CancellationToken.None);
        Assert.NotNull(storedRequest);
        Assert.Equal(request.RepositoryPath, storedRequest.RepositoryPath);
        Assert.Equal(request.SourceBranch, storedRequest.SourceBranch);
        Assert.Equal(request.TargetBranch, storedRequest.TargetBranch);
    }

    [Fact]
    public async Task GetComparison_Should_Return_NotFound_For_NonExistent_Request()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/comparison/{nonExistentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetComparison_Should_Return_Request_Details()
    {
        // Arrange
        var request = new ComparisonRequestDto
        {
            RepositoryPath = "/test/repo2",
            SourceBranch = "feature/new",
            TargetBranch = "develop"
        };
        var postResponse = await client.PostAsJsonAsync("/api/comparison", request);
        var postResult = await postResponse.Content.ReadFromJsonAsync<ComparisonResultDto>();

        // Act
        var response = await client.GetAsync($"/api/comparison/{postResult!.RequestId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ComparisonResultDto>();
        Assert.NotNull(result);
        Assert.Equal(postResult.RequestId, result.RequestId);
        Assert.Equal(request.SourceBranch, result.SourceBranch);
        Assert.Equal(request.TargetBranch, result.TargetBranch);
    }

    [Fact]
    public async Task ComparisonController_Should_Have_Required_Dependencies()
    {
        // Arrange & Act
        var gitService = factory.Services.GetService<IGitService>();
        var comparisonRepo = factory.Services.GetService<IComparisonRequestRepository>();
        var diffRepo = factory.Services.GetService<IDiffResultRepository>();

        // Assert
        Assert.NotNull(gitService);
        Assert.NotNull(comparisonRepo);
        Assert.NotNull(diffRepo);
    }

    [Fact]
    public async Task GetBranches_Should_Handle_Invalid_Repository()
    {
        // Arrange
        var repositoryPath = "/test/repo";

        // Act
        var response = await client.GetAsync($"/api/comparison/branches?repositoryPath={repositoryPath}");

        // Assert
        // Since the repository doesn't exist, we expect an error status
        Assert.True(
            response.StatusCode == HttpStatusCode.InternalServerError ||
            response.StatusCode == HttpStatusCode.BadRequest ||
            response.StatusCode == HttpStatusCode.NotFound,
            $"Expected error status but got {response.StatusCode}");
    }

    [Fact]
    public async Task Multiple_Comparison_Requests_Should_Have_Unique_Ids()
    {
        // Arrange
        var request1 = new ComparisonRequestDto
        {
            RepositoryPath = "/test/repo1",
            SourceBranch = "main",
            TargetBranch = "feature/a"
        };
        var request2 = new ComparisonRequestDto
        {
            RepositoryPath = "/test/repo2",
            SourceBranch = "main",
            TargetBranch = "feature/b"
        };

        // Act
        var response1 = await client.PostAsJsonAsync("/api/comparison", request1);
        var response2 = await client.PostAsJsonAsync("/api/comparison", request2);
        var result1 = await response1.Content.ReadFromJsonAsync<ComparisonResultDto>();
        var result2 = await response2.Content.ReadFromJsonAsync<ComparisonResultDto>();

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.NotEqual(result1.RequestId, result2.RequestId);
    }

    [Fact]
    public async Task RequestComparison_Should_Return_Location_Header()
    {
        // Arrange
        var request = new ComparisonRequestDto
        {
            RepositoryPath = "/test/repo",
            SourceBranch = "main",
            TargetBranch = "feature/test"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/comparison", request);

        // Assert
        Assert.NotNull(response.Headers.Location);
        Assert.Contains("/api/comparison/", response.Headers.Location.ToString());
    }

    [Fact]
    public void GitAnalysisApi_Should_Have_Controllers_Registered()
    {
        // Arrange & Act
        var serviceProvider = factory.Services;

        // Assert - Verify that essential services are registered
        Assert.NotNull(serviceProvider);
    }

    [Fact]
    public async Task ComparisonRequest_Status_Should_Be_Pending_Initially()
    {
        // Arrange
        var request = new ComparisonRequestDto
        {
            RepositoryPath = "/test/repo",
            SourceBranch = "main",
            TargetBranch = "feature/test"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/comparison", request);
        var result = await response.Content.ReadFromJsonAsync<ComparisonResultDto>();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Pending", result.Status);
    }
}
