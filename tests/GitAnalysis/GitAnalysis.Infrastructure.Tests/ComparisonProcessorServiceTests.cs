// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using CodeReviewTool.Shared.Messages;
using CodeReviewTool.Shared.Messaging;
using GitAnalysis.Core.Entities;
using GitAnalysis.Core.Interfaces;
using GitAnalysis.Infrastructure.BackgroundServices;
using Microsoft.Extensions.Logging;
using Moq;

namespace GitAnalysis.Infrastructure.Tests;

public class ComparisonProcessorServiceTests
{
    [Fact]
    public async Task EnqueueRequestAsync_AcceptsRequest()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<ComparisonProcessorService>>();
        var gitServiceMock = new Mock<IGitService>();
        var repositoryMock = new Mock<IComparisonRequestRepository>();
        var diffResultRepositoryMock = new Mock<IDiffResultRepository>();
        var messagePublisherMock = new Mock<IMessagePublisher>();

        var service = new ComparisonProcessorService(
            loggerMock.Object,
            gitServiceMock.Object,
            repositoryMock.Object,
            diffResultRepositoryMock.Object,
            messagePublisherMock.Object);

        // Act
        var requestId = Guid.NewGuid();
        await service.EnqueueRequestAsync(requestId);

        // Assert
        // If no exception is thrown, the test passes
        Assert.True(true);
    }
}
