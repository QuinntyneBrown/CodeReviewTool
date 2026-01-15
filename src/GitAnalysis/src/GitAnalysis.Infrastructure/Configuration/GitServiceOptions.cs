// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace GitAnalysis.Infrastructure.Configuration;

/// <summary>
/// Configuration options for Git service.
/// </summary>
public class GitServiceOptions
{
    /// <summary>
    /// Determines whether to use native git CLI (true) or LibGit2Sharp (false).
    /// Default is true (native git CLI).
    /// </summary>
    public bool UseNativeGit { get; set; } = true;
}
