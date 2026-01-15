// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace GitAnalysis.Core.Services.StaticAnalysis.Models;

/// <summary>
/// Represents a rule violation found during static analysis.
/// </summary>
public class AnalysisViolation
{
    /// <summary>
    /// Gets or sets the rule ID that was violated.
    /// </summary>
    public required string RuleId { get; init; }

    /// <summary>
    /// Gets or sets the specification source (e.g., "message-design.spec.md").
    /// </summary>
    public required string SpecSource { get; init; }

    /// <summary>
    /// Gets or sets the severity of the violation.
    /// </summary>
    public IssueSeverity Severity { get; init; } = IssueSeverity.Error;

    /// <summary>
    /// Gets or sets the message describing the violation.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Gets or sets the file path where the violation was found.
    /// </summary>
    public string? FilePath { get; init; }

    /// <summary>
    /// Gets or sets the line number where the violation was found.
    /// </summary>
    public int? LineNumber { get; init; }

    /// <summary>
    /// Gets or sets the suggested fix for the violation.
    /// </summary>
    public string? SuggestedFix { get; init; }
}
