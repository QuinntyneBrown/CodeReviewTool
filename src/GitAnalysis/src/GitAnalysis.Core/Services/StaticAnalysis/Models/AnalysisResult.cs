// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace GitAnalysis.Core.Services.StaticAnalysis.Models;

/// <summary>
/// Represents the result of a static analysis run.
/// </summary>
public class AnalysisResult
{
    /// <summary>
    /// Gets or sets the root directory that was analyzed.
    /// </summary>
    public required string RootDirectory { get; init; }

    /// <summary>
    /// Gets or sets the type of project detected.
    /// </summary>
    public required ProjectType ProjectType { get; init; }

    /// <summary>
    /// Gets or sets the total number of files analyzed.
    /// </summary>
    public int TotalFilesAnalyzed { get; set; }

    /// <summary>
    /// Gets or sets the list of violations found during analysis.
    /// </summary>
    public List<AnalysisViolation> Violations { get; init; } = [];

    /// <summary>
    /// Gets or sets the list of warnings found during analysis.
    /// </summary>
    public List<AnalysisWarning> Warnings { get; init; } = [];

    /// <summary>
    /// Gets or sets the list of informational messages.
    /// </summary>
    public List<AnalysisInfo> Info { get; init; } = [];

    /// <summary>
    /// Gets a value indicating whether the analysis passed (no violations).
    /// </summary>
    public bool Passed => Violations.Count == 0;

    /// <summary>
    /// Gets or sets the timestamp when the analysis was performed.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
