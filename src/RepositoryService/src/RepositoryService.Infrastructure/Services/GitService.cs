// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using RepositoryService.Core.Interfaces;

namespace RepositoryService.Infrastructure.Services;

public class GitService : IGitService
{
    private readonly ILogger<GitService> _logger;

    public GitService(ILogger<GitService> logger)
    {
        _logger = logger;
    }

    public async Task<IEnumerable<string>> GetBranchesAsync(string repositoryPath, CancellationToken cancellationToken = default)
    {
        var output = await ExecuteGitCommandAsync(repositoryPath, "branch -r", cancellationToken);
        return output
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(b => b.Trim().Replace("origin/", ""))
            .Where(b => !b.Contains("HEAD"))
            .ToList();
    }

    public async Task<IEnumerable<string>> GetCommitsAsync(string repositoryPath, string branch, CancellationToken cancellationToken = default)
    {
        var output = await ExecuteGitCommandAsync(repositoryPath, $"log origin/{branch} --pretty=format:%H -n 100", cancellationToken);
        return output.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList();
    }

    public async Task<string> GetLatestCommitShaAsync(string repositoryPath, string branch, CancellationToken cancellationToken = default)
    {
        var output = await ExecuteGitCommandAsync(repositoryPath, $"rev-parse origin/{branch}", cancellationToken);
        return output.Trim();
    }

    public async Task CloneRepositoryAsync(string url, string localPath, CancellationToken cancellationToken = default)
    {
        if (Directory.Exists(localPath))
        {
            _logger.LogInformation("Repository already exists at {LocalPath}, skipping clone", localPath);
            return;
        }

        var parentDir = Path.GetDirectoryName(localPath) ?? throw new InvalidOperationException("Invalid local path");
        Directory.CreateDirectory(parentDir);

        await ExecuteGitCommandAsync(parentDir, $"clone {url} {Path.GetFileName(localPath)}", cancellationToken);
        _logger.LogInformation("Cloned repository from {Url} to {LocalPath}", url, localPath);
    }

    public async Task PullAsync(string repositoryPath, CancellationToken cancellationToken = default)
    {
        await ExecuteGitCommandAsync(repositoryPath, "fetch --all", cancellationToken);
        _logger.LogDebug("Fetched updates for repository at {RepositoryPath}", repositoryPath);
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

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            _logger.LogError("Git command failed: {Error}", error);
            throw new InvalidOperationException($"Git command failed: {error}");
        }

        return output;
    }
}
