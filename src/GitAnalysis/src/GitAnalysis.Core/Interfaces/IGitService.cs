// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using GitAnalysis.Core.Entities;

namespace GitAnalysis.Core.Interfaces;

/// <summary>
/// Service for Git operations including branch operations and diff generation.
/// </summary>
public interface IGitService
{
    Task<GitDiffResult> GenerateDiffAsync(string repositoryPath, string sourceBranch, string targetBranch, CancellationToken cancellationToken = default);
    Task<bool> BranchExistsAsync(string repositoryPath, string branchName, CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetBranchesAsync(string repositoryPath, CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetFilesInBranchAsync(string repositoryPath, string branchName, CancellationToken cancellationToken = default);
}