// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace GitAnalysis.Core.Services.StaticAnalysis.Models;

/// <summary>
/// Represents the type of project detected.
/// </summary>
public enum ProjectType
{
    /// <summary>
    /// Unknown project type.
    /// </summary>
    Unknown,

    /// <summary>
    /// Git repository.
    /// </summary>
    GitRepository,

    /// <summary>
    /// .NET Solution.
    /// </summary>
    DotNetSolution,

    /// <summary>
    /// Angular workspace.
    /// </summary>
    AngularWorkspace,

    /// <summary>
    /// Node.js environment (package.json present).
    /// </summary>
    NodeEnvironment
}
