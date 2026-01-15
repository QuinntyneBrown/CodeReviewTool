// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using GitAnalysis.Core.Entities;
using GitAnalysis.Core.Interfaces;
using GitAnalysis.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace GitAnalysis.Infrastructure.Tests;

/// <summary>
/// Tests for NativeGitService implementation.
/// </summary>
public class NativeGitServiceTests
{
    private readonly Mock<ILogger<NativeGitService>> loggerMock;
    private readonly Mock<IGitIgnoreEngine> gitIgnoreEngineMock;
    private readonly NativeGitService service;

    public NativeGitServiceTests()
    {
        loggerMock = new Mock<ILogger<NativeGitService>>();
        gitIgnoreEngineMock = new Mock<IGitIgnoreEngine>();
        gitIgnoreEngineMock
            .Setup(x => x.LoadHierarchicalRules(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new List<GitIgnoreRule>());
        gitIgnoreEngineMock
            .Setup(x => x.IsIgnored(It.IsAny<string>(), It.IsAny<IEnumerable<GitIgnoreRule>>()))
            .Returns(false);
        
        service = new NativeGitService(loggerMock.Object, gitIgnoreEngineMock.Object);
    }

    [Fact]
    public void NativeGitService_Constructor_Should_Initialize_With_Dependencies()
    {
        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public void NativeGitService_Constructor_Should_Throw_When_Logger_Is_Null()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new NativeGitService(null!, gitIgnoreEngineMock.Object));
    }

    [Fact]
    public void NativeGitService_Constructor_Should_Throw_When_GitIgnoreEngine_Is_Null()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new NativeGitService(loggerMock.Object, null!));
    }

    [Fact]
    public async Task BranchExistsAsync_Should_Return_False_For_Invalid_Repository()
    {
        // Arrange
        var invalidPath = "/nonexistent/repository";

        // Act
        var result = await service.BranchExistsAsync(invalidPath, "main");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetBranchesAsync_Should_Throw_For_Invalid_Repository()
    {
        // Arrange
        var invalidPath = "/nonexistent/repository";

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await service.GetBranchesAsync(invalidPath));
    }

    [Fact]
    public async Task GetFilesInBranchAsync_Should_Throw_For_Invalid_Repository()
    {
        // Arrange
        var invalidPath = "/nonexistent/repository";

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await service.GetFilesInBranchAsync(invalidPath, "main"));
    }

    [Fact]
    public async Task GenerateDiffAsync_Should_Throw_For_Invalid_Repository()
    {
        // Arrange
        var invalidPath = "/nonexistent/repository";

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await service.GenerateDiffAsync(invalidPath, "main", "feature"));
    }
}
