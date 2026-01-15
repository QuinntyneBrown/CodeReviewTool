// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace GitAnalysis.Core.Services.StaticAnalysis.Scss;

/// <summary>
/// Service for performing static analysis on SCSS files.
/// </summary>
public interface IScssStaticAnalysisService
{
    /// <summary>
    /// Analyzes a single SCSS file.
    /// </summary>
    /// <param name="filePath">The path to the SCSS file.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The analysis result for the file.</returns>
    Task<ScssAnalysisResult> AnalyzeFileAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyzes all SCSS files in a directory.
    /// </summary>
    /// <param name="directoryPath">The path to the directory.</param>
    /// <param name="recursive">Whether to search recursively.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The aggregate analysis result for all files.</returns>
    Task<ScssDirectoryAnalysisResult> AnalyzeDirectoryAsync(string directoryPath, bool recursive = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyzes SCSS content directly.
    /// </summary>
    /// <param name="content">The SCSS content to analyze.</param>
    /// <param name="fileName">An optional file name for context.</param>
    /// <returns>The analysis result.</returns>
    ScssAnalysisResult AnalyzeContent(string content, string fileName = "input.scss");
}
