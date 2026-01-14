// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using GitAnalysis.Core.Entities;

namespace GitAnalysis.Core.Interfaces;

/// <summary>
/// Repository for storing and retrieving Git diff results.
/// </summary>
public interface IDiffResultRepository
{
    Task SaveAsync(Guid requestId, GitDiffResult result, CancellationToken cancellationToken = default);
    Task<GitDiffResult?> GetByRequestIdAsync(Guid requestId, CancellationToken cancellationToken = default);
}
