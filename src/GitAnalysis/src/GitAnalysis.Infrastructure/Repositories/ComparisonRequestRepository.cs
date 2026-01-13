// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Concurrent;
using GitAnalysis.Core.Entities;
using GitAnalysis.Core.Interfaces;

namespace GitAnalysis.Infrastructure.Repositories;

/// <summary>
/// In-memory repository for Git comparison requests.
/// </summary>
public class ComparisonRequestRepository : IComparisonRequestRepository
{
    private readonly ConcurrentDictionary<Guid, GitComparisonRequest> requests = new();

    public Task<GitComparisonRequest> CreateAsync(GitComparisonRequest request, CancellationToken cancellationToken = default)
    {
        request.RequestId = Guid.NewGuid();
        request.RequestedAt = DateTime.UtcNow;
        request.Status = GitComparisonStatus.Pending;

        requests.TryAdd(request.RequestId, request);
        return Task.FromResult(request);
    }

    public Task<GitComparisonRequest?> GetByIdAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        requests.TryGetValue(requestId, out var request);
        return Task.FromResult(request);
    }

    public Task<GitComparisonRequest> UpdateAsync(GitComparisonRequest request, CancellationToken cancellationToken = default)
    {
        requests[request.RequestId] = request;
        return Task.FromResult(request);
    }

    public Task<IEnumerable<GitComparisonRequest>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var userRequests = requests.Values
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.RequestedAt)
            .AsEnumerable();
        
        return Task.FromResult(userRequests);
    }
}