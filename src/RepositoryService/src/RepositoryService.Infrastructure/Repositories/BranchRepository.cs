// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Concurrent;
using RepositoryService.Core.Entities;
using RepositoryService.Core.Interfaces;

namespace RepositoryService.Infrastructure.Repositories;

public class BranchRepository : IBranchRepository
{
    private readonly ConcurrentDictionary<string, Branch> _branches = new();

    public Task<Branch?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        _branches.TryGetValue(id, out var branch);
        return Task.FromResult(branch);
    }

    public Task<IEnumerable<Branch>> GetByRepositoryIdAsync(string repositoryId, CancellationToken cancellationToken = default)
    {
        var branches = _branches.Values.Where(b => b.RepositoryId == repositoryId).ToList();
        return Task.FromResult<IEnumerable<Branch>>(branches);
    }

    public Task<Branch> CreateAsync(Branch branch, CancellationToken cancellationToken = default)
    {
        _branches[branch.Id] = branch;
        return Task.FromResult(branch);
    }

    public Task<Branch> UpdateAsync(Branch branch, CancellationToken cancellationToken = default)
    {
        branch.LastUpdated = DateTime.UtcNow;
        _branches[branch.Id] = branch;
        return Task.FromResult(branch);
    }

    public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        _branches.TryRemove(id, out _);
        return Task.CompletedTask;
    }
}
