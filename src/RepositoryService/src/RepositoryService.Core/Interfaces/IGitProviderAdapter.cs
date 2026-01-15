// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using RepositoryService.Core.Entities;

namespace RepositoryService.Core.Interfaces;

public interface IGitProviderAdapter
{
    GitProvider Provider { get; }
    Task<IEnumerable<PullRequest>> GetPullRequestsAsync(string repositoryId, CancellationToken cancellationToken = default);
    Task<PullRequest?> GetPullRequestAsync(string repositoryId, string number, CancellationToken cancellationToken = default);
}
