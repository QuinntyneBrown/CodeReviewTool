// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using GitAnalysis.Core.Entities;

namespace GitAnalysis.Core.Interfaces;

/// <summary>
/// Engine for parsing and evaluating .gitignore patterns.
/// </summary>
public interface IGitIgnoreEngine
{
    IEnumerable<GitIgnoreRule> ParseGitIgnoreFile(string filePath);
    bool IsIgnored(string filePath, IEnumerable<GitIgnoreRule> rules);
    IEnumerable<GitIgnoreRule> LoadHierarchicalRules(string repositoryPath, string relativePath);
}