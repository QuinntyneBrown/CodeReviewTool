// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace GitAnalysis.Core.Entities;

/// <summary>
/// Result of Git diff operation.
/// </summary>
public class GitDiffResult
{
    public Guid RequestId { get; set; }
    public List<FileDiff> FileDiffs { get; set; } = new();
    public int TotalAdditions { get; set; }
    public int TotalDeletions { get; set; }
    public int TotalModifications { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

public class FileDiff
{
    public required string FilePath { get; set; }
    public FileChangeType ChangeType { get; set; }
    public List<LineDiff> LineChanges { get; set; } = new();
    public int Additions { get; set; }
    public int Deletions { get; set; }
}

public class LineDiff
{
    public int LineNumber { get; set; }
    public required string Content { get; set; }
    public DiffType Type { get; set; }
}

public enum FileChangeType
{
    Added,
    Modified,
    Deleted
}

public enum DiffType
{
    Addition,
    Deletion,
    Context
}