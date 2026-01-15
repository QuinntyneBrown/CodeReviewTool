// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace GitAnalysis.Core.Services.StaticAnalysis.Models;

/// <summary>
/// Represents a warning found during static analysis.
/// </summary>
public class AnalysisWarning
{
    /// <summary>
    /// Gets or sets the rule ID associated with the warning.
    /// </summary>
    public required string RuleId { get; init; }

    /// <summary>
    /// Gets or sets the specification source.
    /// </summary>
    public required string SpecSource { get; init; }

    /// <summary>
    /// Gets or sets the warning message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Gets or sets the file path where the warning was found.
    /// </summary>
    public string? FilePath { get; init; }

    /// <summary>
    /// Gets or sets the line number where the warning was found.
    /// </summary>
    public int? LineNumber { get; init; }

    /// <summary>
    /// Gets or sets the recommendation for addressing the warning.
    /// </summary>
    public string? Recommendation { get; init; }
}
