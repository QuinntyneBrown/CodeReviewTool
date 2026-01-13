// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using GitAnalysis.Core.Entities;

namespace GitAnalysis.Core.Interfaces;

/// <summary>
/// Repository for managing Git comparison requests.
/// </summary>
public interface IComparisonRequestRepository
{
    Task<GitComparisonRequest> CreateAsync(GitComparisonRequest request, CancellationToken cancellationToken = default);
    Task<GitComparisonRequest?> GetByIdAsync(Guid requestId, CancellationToken cancellationToken = default);
    Task<GitComparisonRequest> UpdateAsync(GitComparisonRequest request, CancellationToken cancellationToken = default);
    Task<IEnumerable<GitComparisonRequest>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
}