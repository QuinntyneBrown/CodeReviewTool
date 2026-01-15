// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace GitAnalysis.Core.Services.StaticAnalysis.Models;

/// <summary>
/// Represents the result of a git diff operation.
/// </summary>
public class GitDiffResult
{
    /// <summary>
    /// Gets or sets the current branch name.
    /// </summary>
    public string CurrentBranch { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target branch name (e.g., "main").
    /// </summary>
    public string TargetBranch { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the raw diff output.
    /// </summary>
    public string RawDiff { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of changed files.
    /// </summary>
    public List<string> ChangedFiles { get; set; } = new();

    /// <summary>
    /// Gets or sets whether there are any changes.
    /// </summary>
    public bool HasChanges => ChangedFiles.Count > 0 || !string.IsNullOrWhiteSpace(RawDiff);
}
