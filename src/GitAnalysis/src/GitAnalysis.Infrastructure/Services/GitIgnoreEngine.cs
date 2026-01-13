// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Text.RegularExpressions;
using GitAnalysis.Core.Entities;
using GitAnalysis.Core.Interfaces;

namespace GitAnalysis.Infrastructure.Services;

/// <summary>
/// Robust .gitignore parser that mimics Git's internal logic.
/// Respects hierarchical ignore files and precedence rules.
/// </summary>
public class GitIgnoreEngine : IGitIgnoreEngine
{
    public IEnumerable<GitIgnoreRule> ParseGitIgnoreFile(string filePath)
    {
        if (!File.Exists(filePath))
            yield break;

        foreach (var line in File.ReadLines(filePath))
        {
            var trimmed = line.Trim();
            
            // Skip empty lines and comments
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith('#'))
                continue;

            var isNegation = trimmed.StartsWith('!');
            var pattern = isNegation ? trimmed[1..] : trimmed;
            var isDirectoryOnly = pattern.EndsWith('/');

            if (isDirectoryOnly)
                pattern = pattern[..^1];

            yield return new GitIgnoreRule
            {
                Pattern = pattern,
                IsNegation = isNegation,
                IsDirectoryOnly = isDirectoryOnly,
                SourceFile = filePath
            };
        }
    }

    public bool IsIgnored(string filePath, IEnumerable<GitIgnoreRule> rules)
    {
        var normalizedPath = filePath.Replace('\\', '/');
        var isIgnored = false;

        foreach (var rule in rules)
        {
            if (MatchesPattern(normalizedPath, rule.Pattern))
            {
                isIgnored = !rule.IsNegation;
            }
        }

        return isIgnored;
    }

    public IEnumerable<GitIgnoreRule> LoadHierarchicalRules(string repositoryPath, string relativePath)
    {
        var rules = new List<GitIgnoreRule>();
        var currentPath = repositoryPath;
        var pathParts = string.IsNullOrEmpty(relativePath) 
            ? Array.Empty<string>() 
            : relativePath.Split('/', '\\');

        // Load root .gitignore
        var rootIgnore = Path.Combine(repositoryPath, ".gitignore");
        rules.AddRange(ParseGitIgnoreFile(rootIgnore));

        // Load .gitignore files in subdirectories
        foreach (var part in pathParts)
        {
            currentPath = Path.Combine(currentPath, part);
            var subIgnore = Path.Combine(currentPath, ".gitignore");
            rules.AddRange(ParseGitIgnoreFile(subIgnore));
        }

        return rules;
    }

    private bool MatchesPattern(string path, string pattern)
    {
        // Convert gitignore pattern to regex
        // Handle ** for any directory depth first
        pattern = pattern.Replace("**/", "|||DOUBLESTAR|||");
        
        // Escape the pattern
        var regexPattern = Regex.Escape(pattern);
        
        // Replace placeholders and escaped wildcards
        regexPattern = regexPattern
            .Replace("|||DOUBLESTAR|||", "(.*/)?")
            .Replace("\\*", "[^/]*")
            .Replace("\\?", "[^/]");
        
        regexPattern = "^" + regexPattern + "$";

        return Regex.IsMatch(path, regexPattern, RegexOptions.IgnoreCase);
    }
}