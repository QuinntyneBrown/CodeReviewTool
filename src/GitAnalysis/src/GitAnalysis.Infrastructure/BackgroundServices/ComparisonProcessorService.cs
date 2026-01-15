// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Channels;
using CodeReviewTool.Shared.Messages;
using CodeReviewTool.Shared.Messaging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using GitAnalysis.Core.Entities;
using GitAnalysis.Core.Interfaces;

namespace GitAnalysis.Infrastructure.BackgroundServices;

/// <summary>
/// Background service for asynchronous Git comparison processing.
/// Prevents blocking the API while performing heavy Git operations.
/// </summary>
public class ComparisonProcessorService : BackgroundService
{
    private readonly ILogger<ComparisonProcessorService> logger;
    private readonly IGitService gitService;
    private readonly IComparisonRequestRepository repository;
    private readonly IDiffResultRepository diffResultRepository;
    private readonly IMessagePublisher messagePublisher;
    private readonly Channel<Guid> requestQueue;

    public ComparisonProcessorService(
        ILogger<ComparisonProcessorService> logger,
        IGitService gitService,
        IComparisonRequestRepository repository,
        IDiffResultRepository diffResultRepository,
        IMessagePublisher messagePublisher)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.gitService = gitService ?? throw new ArgumentNullException(nameof(gitService));
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
        this.diffResultRepository = diffResultRepository ?? throw new ArgumentNullException(nameof(diffResultRepository));
        this.messagePublisher = messagePublisher ?? throw new ArgumentNullException(nameof(messagePublisher));
        this.requestQueue = Channel.CreateUnbounded<Guid>();
    }

    public async Task EnqueueRequestAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        await requestQueue.Writer.WriteAsync(requestId, cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Comparison Processor Service starting");

        await foreach (var requestId in requestQueue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                var request = await repository.GetByIdAsync(requestId, stoppingToken);
                if (request == null)
                {
                    logger.LogWarning("Request {RequestId} not found", requestId);
                    continue;
                }

                logger.LogInformation("Processing comparison request {RequestId}", requestId);
                request.Status = GitComparisonStatus.Processing;
                await repository.UpdateAsync(request, stoppingToken);

                // Publish analysis started message
                await messagePublisher.PublishAsync(new AnalysisStartedMessage
                {
                    RequestId = request.RequestId.ToString(),
                    RepositoryPath = request.RepositoryPath,
                    FromBranch = request.FromBranch,
                    IntoBranch = request.IntoBranch
                }, stoppingToken);

                var result = await gitService.GenerateDiffAsync(
                    request.RepositoryPath,
                    request.FromBranch,
                    request.IntoBranch,
                    stoppingToken);

                await diffResultRepository.SaveAsync(request.RequestId, result, stoppingToken);

                request.Status = GitComparisonStatus.Completed;
                request.CompletedAt = DateTime.UtcNow;
                await repository.UpdateAsync(request, stoppingToken);

                // Publish analysis completed message
                await messagePublisher.PublishAsync(new AnalysisCompletedMessage
                {
                    RequestId = request.RequestId.ToString(),
                    RepositoryPath = request.RepositoryPath,
                    CompletedAt = request.CompletedAt.Value
                }, stoppingToken);

                // Publish metrics calculated message
                await messagePublisher.PublishAsync(new AnalysisMetricsCalculatedMessage
                {
                    RequestId = request.RequestId.ToString(),
                    TotalAdditions = result.TotalAdditions,
                    TotalDeletions = result.TotalDeletions,
                    TotalModifications = result.TotalModifications,
                    FilesChanged = result.FileDiffs.Count
                }, stoppingToken);

                logger.LogInformation("Completed comparison request {RequestId}", requestId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing comparison request {RequestId}", requestId);
                
                var request = await repository.GetByIdAsync(requestId, stoppingToken);
                if (request != null)
                {
                    request.Status = GitComparisonStatus.Failed;
                    request.ErrorMessage = ex.Message;
                    request.CompletedAt = DateTime.UtcNow;
                    await repository.UpdateAsync(request, stoppingToken);

                    // Publish analysis failed message
                    await messagePublisher.PublishAsync(new AnalysisFailedMessage
                    {
                        RequestId = request.RequestId.ToString(),
                        RepositoryPath = request.RepositoryPath,
                        ErrorMessage = ex.Message,
                        FailedAt = request.CompletedAt.Value
                    }, stoppingToken);
                }
            }
        }

        logger.LogInformation("Comparison Processor Service stopped");
    }
}