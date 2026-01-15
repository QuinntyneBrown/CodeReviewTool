// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using CodeReviewTool.Shared.Messaging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RepositoryService.Core.Interfaces;
using RepositoryService.Core.Messages;

namespace RepositoryService.Infrastructure.BackgroundServices;

public class RepositoryMonitorService : BackgroundService
{
    private readonly ILogger<RepositoryMonitorService> _logger;
    private readonly IRepositoryRepository _repositoryRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly IGitService _gitService;
    private readonly IMessagePublisher _messagePublisher;

    public RepositoryMonitorService(
        ILogger<RepositoryMonitorService> logger,
        IRepositoryRepository repositoryRepository,
        IBranchRepository branchRepository,
        IGitService gitService,
        IMessagePublisher messagePublisher)
    {
        _logger = logger;
        _repositoryRepository = repositoryRepository;
        _branchRepository = branchRepository;
        _gitService = gitService;
        _messagePublisher = messagePublisher;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Repository Monitor Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await MonitorRepositoriesAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error monitoring repositories");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        _logger.LogInformation("Repository Monitor Service stopped");
    }

    private async Task MonitorRepositoriesAsync(CancellationToken cancellationToken)
    {
        var repositories = await _repositoryRepository.GetAllAsync(cancellationToken);

        foreach (var repository in repositories.Where(r => r.IsActive))
        {
            try
            {
                _logger.LogDebug("Monitoring repository {RepositoryName}", repository.Name);

                if (Directory.Exists(repository.LocalPath))
                {
                    await _gitService.PullAsync(repository.LocalPath, cancellationToken);

                    var branches = await _gitService.GetBranchesAsync(repository.LocalPath, cancellationToken);

                    foreach (var branchName in branches)
                    {
                        var latestSha = await _gitService.GetLatestCommitShaAsync(repository.LocalPath, branchName, cancellationToken);
                        var existingBranches = await _branchRepository.GetByRepositoryIdAsync(repository.Id, cancellationToken);
                        var existingBranch = existingBranches.FirstOrDefault(b => b.Name == branchName);

                        if (existingBranch == null)
                        {
                            var newBranch = new Core.Entities.Branch
                            {
                                RepositoryId = repository.Id,
                                Name = branchName,
                                LatestCommitSha = latestSha
                            };

                            await _branchRepository.CreateAsync(newBranch, cancellationToken);
                        }
                        else if (existingBranch.LatestCommitSha != latestSha)
                        {
                            var commits = await _gitService.GetCommitsAsync(repository.LocalPath, branchName, cancellationToken);
                            var commitsList = commits.ToList();
                            var oldCommitIndex = commitsList.IndexOf(existingBranch.LatestCommitSha);
                            
                            string[] newCommits;
                            if (oldCommitIndex >= 0)
                            {
                                newCommits = commitsList.Take(oldCommitIndex).ToArray();
                            }
                            else
                            {
                                _logger.LogWarning("Previous commit {OldSha} not found in history for {Repository}/{Branch}, this may indicate a force push",
                                    existingBranch.LatestCommitSha, repository.Name, branchName);
                                newCommits = commitsList.Take(10).ToArray();
                            }

                            if (newCommits.Length > 0)
                            {
                                var pushMessage = new RepositoryPushDetectedMessage
                                {
                                    RepositoryId = repository.Id,
                                    Branch = branchName,
                                    CommitShas = newCommits
                                };

                                await _messagePublisher.PublishAsync(pushMessage, cancellationToken);
                                _logger.LogInformation("Detected {Count} new commits in {Repository}/{Branch}",
                                    newCommits.Length, repository.Name, branchName);
                            }

                            existingBranch.LatestCommitSha = latestSha;
                            await _branchRepository.UpdateAsync(existingBranch, cancellationToken);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error monitoring repository {RepositoryName}", repository.Name);
            }
        }
    }
}
