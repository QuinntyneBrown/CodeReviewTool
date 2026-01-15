// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Extensions.Logging;
using RepositoryService.Core.Entities;
using RepositoryService.Core.Interfaces;

namespace RepositoryService.Infrastructure.GitProviders;

public class GitHubAdapter : IGitProviderAdapter
{
    private readonly ILogger<GitHubAdapter> _logger;

    public GitHubAdapter(ILogger<GitHubAdapter> logger)
    {
        _logger = logger;
    }

    public GitProvider Provider => GitProvider.GitHub;

    public async Task<IEnumerable<PullRequest>> GetPullRequestsAsync(string repositoryId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching pull requests for repository {RepositoryId}", repositoryId);
        await Task.CompletedTask;
        return new List<PullRequest>();
    }

    public async Task<PullRequest?> GetPullRequestAsync(string repositoryId, string number, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching pull request {Number} for repository {RepositoryId}", number, repositoryId);
        await Task.CompletedTask;
        return null;
    }
}
