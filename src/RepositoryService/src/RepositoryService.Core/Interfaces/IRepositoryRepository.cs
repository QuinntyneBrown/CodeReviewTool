// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using RepositoryService.Core.Entities;

namespace RepositoryService.Core.Interfaces;

public interface IRepositoryRepository
{
    Task<Repository?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Repository>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Repository> CreateAsync(Repository repository, CancellationToken cancellationToken = default);
    Task<Repository> UpdateAsync(Repository repository, CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
}
