// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using CodeReviewTool.Shared.Messaging;
using GitAnalysis.Core.Interfaces;
using GitAnalysis.Infrastructure.BackgroundServices;
using Microsoft.Extensions.Logging;
using Moq;

namespace GitAnalysis.Infrastructure.Tests;

public class AnalysisRequestHandlerTests
{
    [Fact]
    public void Constructor_WithValidDependencies_CreatesInstance()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<AnalysisRequestHandler>>();
        var messageSubscriberMock = new Mock<IMessageSubscriber>();
        var repositoryMock = new Mock<IComparisonRequestRepository>();
        var processorServiceMock = new Mock<ComparisonProcessorService>(
            Mock.Of<ILogger<ComparisonProcessorService>>(),
            Mock.Of<IGitService>(),
            Mock.Of<IComparisonRequestRepository>(),
            Mock.Of<IDiffResultRepository>(),
            Mock.Of<IMessagePublisher>());

        // Act
        var handler = new AnalysisRequestHandler(
            loggerMock.Object,
            messageSubscriberMock.Object,
            repositoryMock.Object,
            processorServiceMock.Object);

        // Assert
        Assert.NotNull(handler);
    }
}
