// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Concurrent;
using RepositoryService.Core.Entities;
using RepositoryService.Core.Interfaces;

namespace RepositoryService.Infrastructure.Repositories;

public class RepositoryRepository : IRepositoryRepository
{
    private readonly ConcurrentDictionary<string, Repository> _repositories = new();

    public Task<Repository?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        _repositories.TryGetValue(id, out var repository);
        return Task.FromResult(repository);
    }

    public Task<IEnumerable<Repository>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<Repository>>(_repositories.Values.ToList());
    }

    public Task<Repository> CreateAsync(Repository repository, CancellationToken cancellationToken = default)
    {
        _repositories[repository.Id] = repository;
        return Task.FromResult(repository);
    }

    public Task<Repository> UpdateAsync(Repository repository, CancellationToken cancellationToken = default)
    {
        repository.UpdatedAt = DateTime.UtcNow;
        _repositories[repository.Id] = repository;
        return Task.FromResult(repository);
    }

    public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        _repositories.TryRemove(id, out _);
        return Task.CompletedTask;
    }
}
