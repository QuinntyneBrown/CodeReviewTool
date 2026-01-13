// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace GitAnalysis.Core.Entities;

/// <summary>
/// Represents a .gitignore rule with its pattern and negation status.
/// </summary>
public class GitIgnoreRule
{
    public required string Pattern { get; set; }
    public bool IsNegation { get; set; }
    public bool IsDirectoryOnly { get; set; }
    public string? SourceFile { get; set; }
}