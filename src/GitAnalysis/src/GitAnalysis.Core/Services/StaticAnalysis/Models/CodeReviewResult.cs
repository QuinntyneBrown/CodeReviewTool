// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace GitAnalysis.Core.Services.StaticAnalysis.Models;

/// <summary>
/// Represents the result of a code review.
/// </summary>
public class CodeReviewResult
{
    /// <summary>
    /// Gets or sets the git repository root directory.
    /// </summary>
    public string RepositoryRoot { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the git diff result.
    /// </summary>
    public GitDiffResult GitDiff { get; set; } = new();

    /// <summary>
    /// Gets or sets the static analysis result.
    /// </summary>
    public AnalysisResult? AnalysisResult { get; set; }

    /// <summary>
    /// Gets or sets whether the review found any issues.
    /// </summary>
    public bool HasIssues => (AnalysisResult?.Violations.Count ?? 0) > 0;

    /// <summary>
    /// Gets or sets whether the review found any warnings.
    /// </summary>
    public bool HasWarnings => (AnalysisResult?.Warnings.Count ?? 0) > 0;
}
