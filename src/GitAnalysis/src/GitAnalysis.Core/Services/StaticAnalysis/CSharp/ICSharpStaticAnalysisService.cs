// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using GitAnalysis.Core.Services.StaticAnalysis.Models;

namespace GitAnalysis.Core.Services.StaticAnalysis.CSharp;

/// <summary>
/// Represents a single static analysis issue found in the code.
/// </summary>
public class StaticAnalysisIssue
{
    /// <summary>
    /// The file path where the issue was found.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// The line number where the issue was found (1-based).
    /// </summary>
    public int Line { get; set; }

    /// <summary>
    /// The column number where the issue was found (1-based).
    /// </summary>
    public int Column { get; set; }

    /// <summary>
    /// The severity level of the issue.
    /// </summary>
    public IssueSeverity Severity { get; set; }

    /// <summary>
    /// The category of the issue.
    /// </summary>
    public IssueCategory Category { get; set; }

    /// <summary>
    /// A unique rule identifier for the issue.
    /// </summary>
    public string RuleId { get; set; } = string.Empty;

    /// <summary>
    /// A human-readable description of the issue.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// The code snippet where the issue was found.
    /// </summary>
    public string? CodeSnippet { get; set; }

    /// <summary>
    /// A suggested fix for the issue, if available.
    /// </summary>
    public string? SuggestedFix { get; set; }
}

/// <summary>
/// Statistics about a single analyzed file.
/// </summary>
public class FileAnalysisStats
{
    /// <summary>
    /// The file path.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Total lines of code in the file.
    /// </summary>
    public int LinesOfCode { get; set; }

    /// <summary>
    /// Number of classes defined in the file.
    /// </summary>
    public int ClassCount { get; set; }

    /// <summary>
    /// Number of interfaces defined in the file.
    /// </summary>
    public int InterfaceCount { get; set; }

    /// <summary>
    /// Number of methods defined in the file.
    /// </summary>
    public int MethodCount { get; set; }

    /// <summary>
    /// Number of properties defined in the file.
    /// </summary>
    public int PropertyCount { get; set; }

    /// <summary>
    /// Number of issues found in the file.
    /// </summary>
    public int IssueCount { get; set; }
}

/// <summary>
/// Summary statistics for the entire analysis.
/// </summary>
public class AnalysisSummary
{
    /// <summary>
    /// Total number of files analyzed.
    /// </summary>
    public int TotalFiles { get; set; }

    /// <summary>
    /// Total lines of code across all files.
    /// </summary>
    public int TotalLinesOfCode { get; set; }

    /// <summary>
    /// Total number of classes found.
    /// </summary>
    public int TotalClasses { get; set; }

    /// <summary>
    /// Total number of interfaces found.
    /// </summary>
    public int TotalInterfaces { get; set; }

    /// <summary>
    /// Total number of methods found.
    /// </summary>
    public int TotalMethods { get; set; }

    /// <summary>
    /// Total number of issues found.
    /// </summary>
    public int TotalIssues { get; set; }

    /// <summary>
    /// Number of info-level issues.
    /// </summary>
    public int InfoCount { get; set; }

    /// <summary>
    /// Number of warnings.
    /// </summary>
    public int WarningCount { get; set; }

    /// <summary>
    /// Number of errors.
    /// </summary>
    public int ErrorCount { get; set; }

    /// <summary>
    /// Issue counts by category.
    /// </summary>
    public Dictionary<IssueCategory, int> IssuesByCategory { get; set; } = new();

    /// <summary>
    /// Analysis duration in milliseconds.
    /// </summary>
    public long AnalysisDurationMs { get; set; }
}

/// <summary>
/// Options for configuring the static analysis.
/// </summary>
public class CSharpStaticAnalysisOptions
{
    /// <summary>
    /// Whether to include info-level issues.
    /// </summary>
    public bool IncludeInfo { get; set; } = true;

    /// <summary>
    /// Whether to include warnings.
    /// </summary>
    public bool IncludeWarnings { get; set; } = true;

    /// <summary>
    /// Whether to include errors.
    /// </summary>
    public bool IncludeErrors { get; set; } = true;

    /// <summary>
    /// Categories to analyze. If empty, all categories are analyzed.
    /// </summary>
    public HashSet<IssueCategory> Categories { get; set; } = new();

    /// <summary>
    /// Whether to analyze test files.
    /// </summary>
    public bool IncludeTests { get; set; } = false;

    /// <summary>
    /// Maximum number of issues to report. 0 means unlimited.
    /// </summary>
    public int MaxIssues { get; set; } = 0;

    /// <summary>
    /// Creates default options.
    /// </summary>
    public static CSharpStaticAnalysisOptions Default => new();
}

/// <summary>
/// Result of C# static analysis on a codebase.
/// </summary>
public class CSharpStaticAnalysisResult
{
    /// <summary>
    /// The root path that was analyzed.
    /// </summary>
    public string RootPath { get; set; } = string.Empty;

    /// <summary>
    /// The type of analysis target (Solution, Project, or Directory).
    /// </summary>
    public string TargetType { get; set; } = string.Empty;

    /// <summary>
    /// List of issues found during analysis.
    /// </summary>
    public List<StaticAnalysisIssue> Issues { get; set; } = new();

    /// <summary>
    /// Statistics for each analyzed file.
    /// </summary>
    public List<FileAnalysisStats> FileStats { get; set; } = new();

    /// <summary>
    /// Overall summary of the analysis.
    /// </summary>
    public AnalysisSummary Summary { get; set; } = new();

    /// <summary>
    /// Whether the analysis completed successfully.
    /// </summary>
    public bool Success { get; set; } = true;

    /// <summary>
    /// Error message if the analysis failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Generates a formatted string representation of the results.
    /// </summary>
    public string ToFormattedString()
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("╔══════════════════════════════════════════════════════════════════╗");
        sb.AppendLine("║               C# STATIC ANALYSIS REPORT                         ║");
        sb.AppendLine("╚══════════════════════════════════════════════════════════════════╝");
        sb.AppendLine();

        sb.AppendLine($"Target: {RootPath}");
        sb.AppendLine($"Type: {TargetType}");
        sb.AppendLine($"Duration: {Summary.AnalysisDurationMs}ms");
        sb.AppendLine();

        sb.AppendLine("─────────────────────────────────────────────────────────────────────");
        sb.AppendLine("SUMMARY");
        sb.AppendLine("─────────────────────────────────────────────────────────────────────");
        sb.AppendLine($"  Files Analyzed:    {Summary.TotalFiles}");
        sb.AppendLine($"  Lines of Code:     {Summary.TotalLinesOfCode:N0}");
        sb.AppendLine($"  Classes:           {Summary.TotalClasses}");
        sb.AppendLine($"  Interfaces:        {Summary.TotalInterfaces}");
        sb.AppendLine($"  Methods:           {Summary.TotalMethods}");
        sb.AppendLine();
        sb.AppendLine($"  Total Issues:      {Summary.TotalIssues}");
        sb.AppendLine($"    Errors:          {Summary.ErrorCount}");
        sb.AppendLine($"    Warnings:        {Summary.WarningCount}");
        sb.AppendLine($"    Info:            {Summary.InfoCount}");
        sb.AppendLine();

        if (Summary.IssuesByCategory.Count > 0)
        {
            sb.AppendLine("  Issues by Category:");
            foreach (var kvp in Summary.IssuesByCategory.OrderByDescending(x => x.Value))
            {
                sb.AppendLine($"    {kvp.Key,-20} {kvp.Value}");
            }
            sb.AppendLine();
        }

        if (Issues.Count > 0)
        {
            sb.AppendLine("─────────────────────────────────────────────────────────────────────");
            sb.AppendLine("ISSUES");
            sb.AppendLine("─────────────────────────────────────────────────────────────────────");

            var groupedByFile = Issues.GroupBy(i => i.FilePath).OrderBy(g => g.Key);
            foreach (var fileGroup in groupedByFile)
            {
                var relativePath = Path.GetRelativePath(RootPath, fileGroup.Key);
                sb.AppendLine();
                sb.AppendLine($"📄 {relativePath}");

                foreach (var issue in fileGroup.OrderBy(i => i.Line))
                {
                    var severityIcon = issue.Severity switch
                    {
                        IssueSeverity.Error => "❌",
                        IssueSeverity.Warning => "⚠️",
                        _ => "ℹ️"
                    };

                    sb.AppendLine($"  {severityIcon} [{issue.RuleId}] Line {issue.Line}: {issue.Message}");

                    if (!string.IsNullOrEmpty(issue.CodeSnippet))
                    {
                        sb.AppendLine($"     Code: {issue.CodeSnippet.Trim()}");
                    }

                    if (!string.IsNullOrEmpty(issue.SuggestedFix))
                    {
                        sb.AppendLine($"     Fix: {issue.SuggestedFix}");
                    }
                }
            }
        }
        else
        {
            sb.AppendLine("─────────────────────────────────────────────────────────────────────");
            sb.AppendLine("✅ No issues found!");
            sb.AppendLine("─────────────────────────────────────────────────────────────────────");
        }

        sb.AppendLine();
        sb.AppendLine("═════════════════════════════════════════════════════════════════════");

        return sb.ToString();
    }
}

/// <summary>
/// Service for performing static analysis on C# code.
/// </summary>
public interface ICSharpStaticAnalysisService
{
    /// <summary>
    /// Analyzes C# code at the specified path.
    /// </summary>
    /// <param name="path">The path to analyze (solution, project, or directory).</param>
    /// <param name="options">Analysis options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The analysis result.</returns>
    Task<CSharpStaticAnalysisResult> AnalyzeAsync(
        string path,
        CSharpStaticAnalysisOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines the type of target at the specified path.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <returns>The target type (Solution, Project, Directory, or File).</returns>
    string DetermineTargetType(string path);

    /// <summary>
    /// Finds the root solution or project from the specified path.
    /// </summary>
    /// <param name="path">The starting path.</param>
    /// <returns>The root path and its type.</returns>
    (string RootPath, string TargetType) FindRoot(string path);
}
