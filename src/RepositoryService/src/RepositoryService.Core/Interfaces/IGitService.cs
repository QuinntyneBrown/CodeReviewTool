// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace RepositoryService.Core.Interfaces;

public interface IGitService
{
    Task<IEnumerable<string>> GetBranchesAsync(string repositoryPath, CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetCommitsAsync(string repositoryPath, string branch, CancellationToken cancellationToken = default);
    Task<string> GetLatestCommitShaAsync(string repositoryPath, string branch, CancellationToken cancellationToken = default);
    Task CloneRepositoryAsync(string url, string localPath, CancellationToken cancellationToken = default);
    Task PullAsync(string repositoryPath, CancellationToken cancellationToken = default);
}
