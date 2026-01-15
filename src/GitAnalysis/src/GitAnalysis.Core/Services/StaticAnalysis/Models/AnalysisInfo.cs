// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace GitAnalysis.Core.Services.StaticAnalysis.Models;

/// <summary>
/// Represents an informational message from static analysis.
/// </summary>
public class AnalysisInfo
{
    /// <summary>
    /// Gets or sets the category of the information.
    /// </summary>
    public required string Category { get; init; }

    /// <summary>
    /// Gets or sets the informational message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Gets or sets the file path related to the information.
    /// </summary>
    public string? FilePath { get; init; }
}
