// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace GitAnalysis.Core.Services.StaticAnalysis.Models;

/// <summary>
/// Represents the severity level of a static analysis issue.
/// This is the canonical severity enum used across all static analysis services.
/// </summary>
public enum IssueSeverity
{
    /// <summary>
    /// Informational message, not necessarily a problem.
    /// </summary>
    Info = 0,

    /// <summary>
    /// Warning that should be addressed.
    /// </summary>
    Warning = 1,

    /// <summary>
    /// Error that must be fixed.
    /// </summary>
    Error = 2
}

/// <summary>
/// Category of static analysis issue.
/// </summary>
public enum IssueCategory
{
    /// <summary>
    /// Naming convention violations.
    /// </summary>
    Naming,

    /// <summary>
    /// Code style and formatting issues.
    /// </summary>
    Style,

    /// <summary>
    /// Potential bugs or code quality issues.
    /// </summary>
    CodeQuality,

    /// <summary>
    /// Unused code or imports.
    /// </summary>
    UnusedCode,

    /// <summary>
    /// Documentation issues.
    /// </summary>
    Documentation,

    /// <summary>
    /// Design and architecture concerns.
    /// </summary>
    Design,

    /// <summary>
    /// Performance-related issues.
    /// </summary>
    Performance,

    /// <summary>
    /// Security-related issues.
    /// </summary>
    Security,

    /// <summary>
    /// Maintainability concerns.
    /// </summary>
    Maintainability
}
