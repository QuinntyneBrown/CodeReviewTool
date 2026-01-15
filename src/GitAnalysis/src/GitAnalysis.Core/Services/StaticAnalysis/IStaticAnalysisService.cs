// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using GitAnalysis.Core.Services.StaticAnalysis.Models;

namespace GitAnalysis.Core.Services.StaticAnalysis;

/// <summary>
/// Service for performing static analysis on code repositories.
/// </summary>
public interface IStaticAnalysisService
{
    /// <summary>
    /// Analyzes a project at the specified file path.
    /// </summary>
    /// <param name="filePath">The file path to analyze (can be any path within the project).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The analysis result.</returns>
    Task<AnalysisResult> AnalyzeAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines the project root directory from a given file path.
    /// </summary>
    /// <param name="filePath">The file path to start from.</param>
    /// <returns>A tuple containing the root directory and project type, or null if no project root found.</returns>
    (string RootDirectory, ProjectType ProjectType)? DetermineProjectRoot(string filePath);
}
