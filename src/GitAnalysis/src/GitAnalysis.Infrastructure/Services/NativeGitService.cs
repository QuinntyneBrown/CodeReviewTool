// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Diagnostics;
using System.Text.RegularExpressions;
using GitAnalysis.Core.Entities;
using GitAnalysis.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace GitAnalysis.Infrastructure.Services;

/// <summary>
/// Git service implementation using native git command-line tool.
/// Provides branch isolation, switching, and diff generation capabilities without external libraries.
/// </summary>
public class NativeGitService : IGitService
{
    private readonly ILogger<NativeGitService> logger;
    private readonly IGitIgnoreEngine gitIgnoreEngine;
    private static readonly Regex HunkHeaderRegex = 
        new(@"@@ -\d+(?:,\d+)? \+(\d+)", RegexOptions.Compiled);
    private const string GitDiffPathPrefix = "a/";

    public NativeGitService(ILogger<NativeGitService> logger, IGitIgnoreEngine gitIgnoreEngine)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.gitIgnoreEngine = gitIgnoreEngine ?? throw new ArgumentNullException(nameof(gitIgnoreEngine));
    }

    public async Task<GitDiffResult> GenerateDiffAsync(string repositoryPath, string fromBranch, string intoBranch, CancellationToken cancellationToken = default)
    {
        // If fromBranch is empty/null, use current branch
        if (string.IsNullOrWhiteSpace(fromBranch))
        {
            fromBranch = await GetCurrentBranchAsync(repositoryPath, cancellationToken);
        }

        // If intoBranch is empty/null, use main/master
        if (string.IsNullOrWhiteSpace(intoBranch))
        {
            intoBranch = await GetMainBranchAsync(repositoryPath, cancellationToken);
        }

        logger.LogInformation("Generating diff between {From} and {Into} in {Repo}", 
            fromBranch, intoBranch, repositoryPath);

        var result = new GitDiffResult { RequestId = Guid.NewGuid() };

        // Get the diff with numstat for file statistics
        var numStatOutput = await ExecuteGitCommandAsync(
            repositoryPath, 
            $"diff --numstat {intoBranch}..{fromBranch}", 
            cancellationToken);

        // Get the full diff patch
        var patchOutput = await ExecuteGitCommandAsync(
            repositoryPath,
            $"diff {intoBranch}..{fromBranch}",
            cancellationToken);

        var ignoreRules = gitIgnoreEngine.LoadHierarchicalRules(repositoryPath, string.Empty);

        // Parse numstat output to get file statistics
        var fileStats = ParseNumStat(numStatOutput);

        // Parse the patch to get detailed line changes
        var fileDiffs = ParsePatch(patchOutput, fileStats, ignoreRules);

        foreach (var fileDiff in fileDiffs)
        {
            result.FileDiffs.Add(fileDiff);
            result.TotalAdditions += fileDiff.Additions;
            result.TotalDeletions += fileDiff.Deletions;
            if (fileDiff.ChangeType == FileChangeType.Modified)
                result.TotalModifications++;
        }

        logger.LogInformation("Generated diff between {From} and {Into}: {Files} files changed, +{Additions} -{Deletions}",
            fromBranch, intoBranch, result.FileDiffs.Count, result.TotalAdditions, result.TotalDeletions);

        return result;
    }

    public async Task<bool> BranchExistsAsync(string repositoryPath, string branchName, CancellationToken cancellationToken = default)
    {
        try
        {
            var output = await ExecuteGitCommandAsync(
                repositoryPath,
                $"rev-parse --verify {branchName}",
                cancellationToken);
            return !string.IsNullOrWhiteSpace(output);
        }
        catch
        {
            return false;
        }
    }

    public async Task<IEnumerable<string>> GetBranchesAsync(string repositoryPath, CancellationToken cancellationToken = default)
    {
        var output = await ExecuteGitCommandAsync(
            repositoryPath,
            "branch -a",
            cancellationToken);

        return output
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(b => b.Trim().TrimStart('*').Trim())
            .Where(b => !string.IsNullOrWhiteSpace(b))
            .Select(b => b.Replace("remotes/origin/", ""))
            .Distinct()
            .ToList();
    }

    public async Task<IEnumerable<string>> GetFilesInBranchAsync(string repositoryPath, string branchName, CancellationToken cancellationToken = default)
    {
        var output = await ExecuteGitCommandAsync(
            repositoryPath,
            $"ls-tree -r --name-only {branchName}",
            cancellationToken);

        return output
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(f => f.Trim())
            .Where(f => !string.IsNullOrWhiteSpace(f))
            .ToList();
    }

    private async Task<string> GetCurrentBranchAsync(string repositoryPath, CancellationToken cancellationToken)
    {
        var output = await ExecuteGitCommandAsync(
            repositoryPath,
            "rev-parse --abbrev-ref HEAD",
            cancellationToken);
        return output.Trim();
    }

    private async Task<string> GetMainBranchAsync(string repositoryPath, CancellationToken cancellationToken)
    {
        // Try to find main branch (main or master)
        var branches = await GetBranchesAsync(repositoryPath, cancellationToken);
        
        if (branches.Contains("main"))
            return "main";
        if (branches.Contains("master"))
            return "master";
        
        // Fallback to symbolic-ref
        try
        {
            var output = await ExecuteGitCommandAsync(
                repositoryPath,
                "symbolic-ref refs/remotes/origin/HEAD",
                cancellationToken);
            return output.Replace("refs/remotes/origin/", "").Trim();
        }
        catch
        {
            // Default to main
            return "main";
        }
    }

    private Dictionary<string, (int additions, int deletions, FileChangeType changeType)> ParseNumStat(string numStatOutput)
    {
        var stats = new Dictionary<string, (int, int, FileChangeType)>();
        
        if (string.IsNullOrWhiteSpace(numStatOutput))
            return stats;

        foreach (var line in numStatOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Split('\t', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3)
                continue;

            // Handle binary files and non-numeric values safely
            if (!int.TryParse(parts[0], out var additions))
                additions = 0;
            if (!int.TryParse(parts[1], out var deletions))
                deletions = 0;
            
            var filePath = parts[2];

            // Determine change type based on additions and deletions
            FileChangeType changeType;
            if (additions > 0 && deletions == 0)
                changeType = FileChangeType.Added;
            else if (additions == 0 && deletions > 0)
                changeType = FileChangeType.Deleted;
            else
                changeType = FileChangeType.Modified;

            stats[filePath] = (additions, deletions, changeType);
        }

        return stats;
    }

    private List<FileDiff> ParsePatch(
        string patchOutput, 
        Dictionary<string, (int additions, int deletions, FileChangeType changeType)> fileStats,
        IEnumerable<Core.Entities.GitIgnoreRule> ignoreRules)
    {
        var fileDiffs = new List<FileDiff>();
        
        if (string.IsNullOrWhiteSpace(patchOutput))
            return fileDiffs;

        var lines = patchOutput.Split('\n');
        FileDiff? currentFileDiff = null;
        int currentLineNumber = 0;

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];

            // New file diff starts with "diff --git"
            if (line.StartsWith("diff --git"))
            {
                // Save previous file diff
                if (currentFileDiff != null && !gitIgnoreEngine.IsIgnored(currentFileDiff.FilePath, ignoreRules))
                {
                    fileDiffs.Add(currentFileDiff);
                }

                // Extract file path (format: diff --git a/path b/path)
                var parts = line.Split(' ');
                if (parts.Length >= 4)
                {
                    // Remove git diff path prefix (parts[2] is like 'a/path/to/file')
                    var filePath = RemoveGitPathPrefix(parts[2]);
                    
                    if (fileStats.TryGetValue(filePath, out var stats))
                    {
                        currentFileDiff = new FileDiff
                        {
                            FilePath = filePath,
                            ChangeType = stats.changeType,
                            Additions = stats.additions,
                            Deletions = stats.deletions
                        };
                    }
                    else
                    {
                        currentFileDiff = new FileDiff
                        {
                            FilePath = filePath,
                            ChangeType = FileChangeType.Modified,
                            Additions = 0,
                            Deletions = 0
                        };
                    }
                }
                currentLineNumber = 0;
            }
            // Parse hunk header to get line number context
            else if (line.StartsWith("@@"))
            {
                // Format: @@ -old_start,old_count +new_start,new_count @@
                var match = HunkHeaderRegex.Match(line);
                if (match.Success)
                {
                    currentLineNumber = int.Parse(match.Groups[1].Value);
                }
            }
            // Parse actual changes
            else if (currentFileDiff != null && line.Length > 0)
            {
                if (line.StartsWith('+') && !line.StartsWith("+++"))
                {
                    currentFileDiff.LineChanges.Add(new LineDiff
                    {
                        LineNumber = currentLineNumber++,
                        Content = line.Length > 1 ? line[1..] : string.Empty,
                        Type = DiffType.Addition
                    });
                }
                else if (line.StartsWith('-') && !line.StartsWith("---"))
                {
                    currentFileDiff.LineChanges.Add(new LineDiff
                    {
                        LineNumber = currentLineNumber,
                        Content = line.Length > 1 ? line[1..] : string.Empty,
                        Type = DiffType.Deletion
                    });
                }
                else if (!line.StartsWith('\\')) // Skip "\ No newline at end of file"
                {
                    currentLineNumber++;
                }
            }
        }

        // Don't forget the last file
        if (currentFileDiff != null && !gitIgnoreEngine.IsIgnored(currentFileDiff.FilePath, ignoreRules))
        {
            fileDiffs.Add(currentFileDiff);
        }

        return fileDiffs;
    }

    private static string RemoveGitPathPrefix(string path)
    {
        return path.StartsWith(GitDiffPathPrefix) ? path[GitDiffPathPrefix.Length..] : path;
    }

    private async Task<string> ExecuteGitCommandAsync(string workingDirectory, string arguments, CancellationToken cancellationToken)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = processStartInfo };
        process.Start();

        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

        // Await all tasks concurrently to prevent buffer deadlock
        await Task.WhenAll(outputTask, errorTask, process.WaitForExitAsync(cancellationToken));

        var output = await outputTask;
        var error = await errorTask;

        if (process.ExitCode != 0)
        {
            logger.LogError("Git command failed: {Command}. Error: {Error}", arguments, error);
            throw new InvalidOperationException($"Git command failed: {error}");
        }

        return output;
    }
}
