// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using GitAnalysis.Core.Services.StaticAnalysis.Models;

namespace GitAnalysis.Core.Services.StaticAnalysis.Scss;

/// <summary>
/// Represents the result of SCSS static analysis.
/// </summary>
public class ScssAnalysisResult
{
    /// <summary>
    /// Gets or sets the path to the analyzed file.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of issues found during analysis.
    /// </summary>
    public List<ScssIssue> Issues { get; set; } = new();

    /// <summary>
    /// Gets or sets the total number of lines analyzed.
    /// </summary>
    public int TotalLines { get; set; }

    /// <summary>
    /// Gets or sets the number of selectors found.
    /// </summary>
    public int SelectorCount { get; set; }

    /// <summary>
    /// Gets or sets the number of variables found.
    /// </summary>
    public int VariableCount { get; set; }

    /// <summary>
    /// Gets or sets the number of mixins found.
    /// </summary>
    public int MixinCount { get; set; }

    /// <summary>
    /// Gets or sets the maximum nesting depth found.
    /// </summary>
    public int MaxNestingDepth { get; set; }

    /// <summary>
    /// Gets a value indicating whether the file has any issues.
    /// </summary>
    public bool HasIssues => Issues.Count > 0;

    /// <summary>
    /// Gets the count of errors.
    /// </summary>
    public int ErrorCount => Issues.Count(i => i.Severity == IssueSeverity.Error);

    /// <summary>
    /// Gets the count of warnings.
    /// </summary>
    public int WarningCount => Issues.Count(i => i.Severity == IssueSeverity.Warning);

    /// <summary>
    /// Gets the count of informational issues.
    /// </summary>
    public int InfoCount => Issues.Count(i => i.Severity == IssueSeverity.Info);
}

/// <summary>
/// Represents an issue found during SCSS static analysis.
/// </summary>
public class ScssIssue
{
    /// <summary>
    /// Gets or sets the line number where the issue was found.
    /// </summary>
    public int Line { get; set; }

    /// <summary>
    /// Gets or sets the column number where the issue was found.
    /// </summary>
    public int Column { get; set; }

    /// <summary>
    /// Gets or sets the issue code.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the message describing the issue.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the severity of the issue.
    /// </summary>
    public IssueSeverity Severity { get; set; }

    /// <summary>
    /// Gets or sets the rule that triggered the issue.
    /// </summary>
    public string Rule { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the source code snippet where the issue was found.
    /// </summary>
    public string? SourceSnippet { get; set; }
}

/// <summary>
/// Represents the aggregate result of analyzing multiple SCSS files.
/// </summary>
public class ScssDirectoryAnalysisResult
{
    /// <summary>
    /// Gets or sets the directory that was analyzed.
    /// </summary>
    public string DirectoryPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of file analysis results.
    /// </summary>
    public List<ScssAnalysisResult> FileResults { get; set; } = new();

    /// <summary>
    /// Gets the total number of files analyzed.
    /// </summary>
    public int TotalFiles => FileResults.Count;

    /// <summary>
    /// Gets the total number of issues found.
    /// </summary>
    public int TotalIssues => FileResults.Sum(r => r.Issues.Count);

    /// <summary>
    /// Gets the total number of errors found.
    /// </summary>
    public int TotalErrors => FileResults.Sum(r => r.ErrorCount);

    /// <summary>
    /// Gets the total number of warnings found.
    /// </summary>
    public int TotalWarnings => FileResults.Sum(r => r.WarningCount);

    /// <summary>
    /// Gets the total number of lines analyzed.
    /// </summary>
    public int TotalLines => FileResults.Sum(r => r.TotalLines);

    /// <summary>
    /// Gets a value indicating whether any files have issues.
    /// </summary>
    public bool HasIssues => TotalIssues > 0;
}
