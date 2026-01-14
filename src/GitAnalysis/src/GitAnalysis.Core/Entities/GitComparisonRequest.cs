// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace GitAnalysis.Core.Entities;

/// <summary>
/// Request for Git comparison between two branches.
/// </summary>
public class GitComparisonRequest
{
    public Guid RequestId { get; set; }
    public required string RepositoryPath { get; set; }
    public required string FromBranch { get; set; }
    public required string IntoBranch { get; set; }
    public string? UserId { get; set; }
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public GitComparisonStatus Status { get; set; } = GitComparisonStatus.Pending;
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
}

public enum GitComparisonStatus
{
    Pending,
    Processing,
    Completed,
    Failed
}