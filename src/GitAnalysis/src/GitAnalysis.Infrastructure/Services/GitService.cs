// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using LibGit2Sharp;
using GitAnalysis.Core.Entities;
using GitAnalysis.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace GitAnalysis.Infrastructure.Services;

/// <summary>
/// Git service implementation using LibGit2Sharp.
/// Provides branch isolation, switching, and diff generation capabilities.
/// </summary>
public class GitService : IGitService
{
    private readonly ILogger<GitService> logger;
    private readonly IGitIgnoreEngine gitIgnoreEngine;

    public GitService(ILogger<GitService> logger, IGitIgnoreEngine gitIgnoreEngine)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.gitIgnoreEngine = gitIgnoreEngine ?? throw new ArgumentNullException(nameof(gitIgnoreEngine));
    }

    public async Task<GitDiffResult> GenerateDiffAsync(string repositoryPath, string fromBranch, string intoBranch, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            using var repo = new Repository(repositoryPath);
            
            var sourceCommit = repo.Branches[fromBranch]?.Tip 
                ?? throw new InvalidOperationException($"Branch '{fromBranch}' not found");
            var targetCommit = repo.Branches[intoBranch]?.Tip 
                ?? throw new InvalidOperationException($"Branch '{intoBranch}' not found");

            var diff = repo.Diff.Compare<Patch>(targetCommit.Tree, sourceCommit.Tree);
            var result = new GitDiffResult { RequestId = Guid.NewGuid() };

            var ignoreRules = gitIgnoreEngine.LoadHierarchicalRules(repositoryPath, string.Empty);

            foreach (var change in diff)
            {
                if (gitIgnoreEngine.IsIgnored(change.Path, ignoreRules))
                    continue;

                var fileDiff = new FileDiff
                {
                    FilePath = change.Path,
                    ChangeType = MapChangeType(change.Status),
                    Additions = change.LinesAdded,
                    Deletions = change.LinesDeleted
                };

                foreach (var line in change.Patch.Split('\n').Select((content, index) => new { content, index }))
                {
                    if (line.content.StartsWith('+') && !line.content.StartsWith("+++"))
                    {
                        fileDiff.LineChanges.Add(new LineDiff
                        {
                            LineNumber = line.index,
                            Content = line.content[1..],
                            Type = DiffType.Addition
                        });
                    }
                    else if (line.content.StartsWith('-') && !line.content.StartsWith("---"))
                    {
                        fileDiff.LineChanges.Add(new LineDiff
                        {
                            LineNumber = line.index,
                            Content = line.content[1..],
                            Type = DiffType.Deletion
                        });
                    }
                }

                result.FileDiffs.Add(fileDiff);
                result.TotalAdditions += fileDiff.Additions;
                result.TotalDeletions += fileDiff.Deletions;
                if (fileDiff.ChangeType == FileChangeType.Modified)
                    result.TotalModifications++;
            }

            logger.LogInformation("Generated diff between {From} and {Into}: {Files} files changed",
                fromBranch, intoBranch, result.FileDiffs.Count);

            return result;
        }, cancellationToken);
    }

    public Task<bool> BranchExistsAsync(string repositoryPath, string branchName, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            using var repo = new Repository(repositoryPath);
            return repo.Branches[branchName] != null;
        }, cancellationToken);
    }

    public Task<IEnumerable<string>> GetBranchesAsync(string repositoryPath, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            using var repo = new Repository(repositoryPath);
            return repo.Branches.Select(b => b.FriendlyName).ToList().AsEnumerable();
        }, cancellationToken);
    }

    public Task<IEnumerable<string>> GetFilesInBranchAsync(string repositoryPath, string branchName, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            using var repo = new Repository(repositoryPath);
            var branch = repo.Branches[branchName]
                ?? throw new InvalidOperationException($"Branch '{branchName}' not found");

            return branch.Tip.Tree
                .Select(entry => entry.Path)
                .ToList()
                .AsEnumerable();
        }, cancellationToken);
    }

    private static FileChangeType MapChangeType(ChangeKind changeKind) => changeKind switch
    {
        ChangeKind.Added => FileChangeType.Added,
        ChangeKind.Deleted => FileChangeType.Deleted,
        ChangeKind.Modified => FileChangeType.Modified,
        _ => FileChangeType.Modified
    };
}