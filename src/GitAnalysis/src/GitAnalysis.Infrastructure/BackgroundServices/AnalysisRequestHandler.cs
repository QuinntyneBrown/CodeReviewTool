// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using CodeReviewTool.Shared.Messages;
using CodeReviewTool.Shared.Messaging;
using GitAnalysis.Core.Entities;
using GitAnalysis.Core.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GitAnalysis.Infrastructure.BackgroundServices;

/// <summary>
/// Background service that subscribes to analysis.requested messages
/// and enqueues them to the ComparisonProcessorService.
/// </summary>
public class AnalysisRequestHandler : BackgroundService
{
    private readonly ILogger<AnalysisRequestHandler> logger;
    private readonly IMessageSubscriber messageSubscriber;
    private readonly IComparisonRequestRepository repository;
    private readonly ComparisonProcessorService processorService;
    private CancellationToken _stoppingToken;

    public AnalysisRequestHandler(
        ILogger<AnalysisRequestHandler> logger,
        IMessageSubscriber messageSubscriber,
        IComparisonRequestRepository repository,
        ComparisonProcessorService processorService)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.messageSubscriber = messageSubscriber ?? throw new ArgumentNullException(nameof(messageSubscriber));
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
        this.processorService = processorService ?? throw new ArgumentNullException(nameof(processorService));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _stoppingToken = stoppingToken;
        logger.LogInformation("Analysis Request Handler starting");

        try
        {
            await messageSubscriber.SubscribeAsync<AnalysisRequestedMessage>(
                HandleAnalysisRequestedAsync,
                stoppingToken);

            logger.LogInformation("Subscribed to AnalysisRequestedMessage");

            // Keep the service running
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Analysis Request Handler stopping");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in Analysis Request Handler");
        }
    }

    private async Task HandleAnalysisRequestedAsync(AnalysisRequestedMessage message)
    {
        try
        {
            logger.LogInformation(
                "Received analysis request for {Repo} between {From} and {Into}",
                message.RepositoryPath,
                message.FromBranch,
                message.IntoBranch);

            var request = new GitComparisonRequest
            {
                RepositoryPath = message.RepositoryPath,
                FromBranch = message.FromBranch,
                IntoBranch = message.IntoBranch,
                UserId = message.RequestedBy
            };

            request = await repository.CreateAsync(request, _stoppingToken);
            await processorService.EnqueueRequestAsync(request.RequestId, _stoppingToken);

            logger.LogInformation("Enqueued analysis request {RequestId}", request.RequestId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling AnalysisRequestedMessage");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Analysis Request Handler stopping");
        
        try
        {
            await messageSubscriber.UnsubscribeAsync<AnalysisRequestedMessage>();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error unsubscribing from AnalysisRequestedMessage");
        }

        await base.StopAsync(cancellationToken);
    }
}
