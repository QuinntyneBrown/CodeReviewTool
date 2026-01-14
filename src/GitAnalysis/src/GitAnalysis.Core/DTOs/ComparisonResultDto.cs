// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace GitAnalysis.Core.DTOs;

/// <summary>
/// DTO for Git comparison result.
/// </summary>
public class ComparisonResultDto
{
    public Guid RequestId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string SourceBranch { get; set; } = string.Empty;
    public string TargetBranch { get; set; } = string.Empty;
    public List<FileDiffDto> FileDiffs { get; set; } = new();
    public int TotalAdditions { get; set; }
    public int TotalDeletions { get; set; }
    public int TotalModifications { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
}

public class FileDiffDto
{
    public required string FilePath { get; set; }
    public string ChangeType { get; set; } = string.Empty;
    public int Additions { get; set; }
    public int Deletions { get; set; }
    public List<LineDiffDto> LineChanges { get; set; } = new();
}

public class LineDiffDto
{
    public int LineNumber { get; set; }
    public string Content { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}