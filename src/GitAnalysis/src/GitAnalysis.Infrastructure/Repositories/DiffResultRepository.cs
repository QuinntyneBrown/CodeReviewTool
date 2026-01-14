// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Concurrent;
using GitAnalysis.Core.Entities;
using GitAnalysis.Core.Interfaces;

namespace GitAnalysis.Infrastructure.Repositories;

/// <summary>
/// In-memory repository for Git diff results.
/// </summary>
public class DiffResultRepository : IDiffResultRepository
{
    private readonly ConcurrentDictionary<Guid, GitDiffResult> results = new();

    public Task SaveAsync(Guid requestId, GitDiffResult result, CancellationToken cancellationToken = default)
    {
        result.RequestId = requestId;
        results[requestId] = result;
        return Task.CompletedTask;
    }

    public Task<GitDiffResult?> GetByRequestIdAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        results.TryGetValue(requestId, out var result);
        return Task.FromResult(result);
    }
}
