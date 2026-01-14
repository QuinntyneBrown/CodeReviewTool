using System.Diagnostics;

namespace CommandLine.Console;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        var currentDirectory = Directory.GetCurrentDirectory();

        Console.WriteLine($"Working directory: {currentDirectory}");
        Console.WriteLine();

        // Check if we're in a git repository
        if (!await IsGitRepository(currentDirectory))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Error: Not a git repository.");
            Console.ResetColor();
            return 1;
        }

        // Get the current branch name
        var currentBranch = await GetCurrentBranch(currentDirectory);
        if (string.IsNullOrEmpty(currentBranch))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Error: Could not determine current branch.");
            Console.ResetColor();
            return 1;
        }

        Console.WriteLine($"Current branch: {currentBranch}");

        // Determine the main branch name (could be 'main' or 'master')
        var mainBranch = await GetMainBranchName(currentDirectory);
        if (string.IsNullOrEmpty(mainBranch))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Error: Could not find main or master branch.");
            Console.ResetColor();
            return 1;
        }

        Console.WriteLine($"Main branch: {mainBranch}");
        Console.WriteLine();

        if (currentBranch == mainBranch)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Warning: You are already on the main branch. No diff to show.");
            Console.ResetColor();
            return 0;
        }

        // Fetch the latest from remote to ensure we have up-to-date refs
        Console.WriteLine("Fetching latest from remote...");
        await RunGitCommand(currentDirectory, "fetch", "origin");
        Console.WriteLine();

        // Get the diff between current branch and main
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"=== Diff: {currentBranch} vs {mainBranch} ===");
        Console.ResetColor();
        Console.WriteLine();

        var diff = await GetDiff(currentDirectory, mainBranch, currentBranch);

        if (string.IsNullOrWhiteSpace(diff))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("No differences found between branches.");
            Console.ResetColor();
        }
        else
        {
            PrintColorizedDiff(diff);
        }

        return 0;
    }

    private static async Task<bool> IsGitRepository(string directory)
    {
        var result = await RunGitCommand(directory, "rev-parse", "--is-inside-work-tree");
        return result.ExitCode == 0 && result.Output.Trim() == "true";
    }

    private static async Task<string> GetCurrentBranch(string directory)
    {
        var result = await RunGitCommand(directory, "rev-parse", "--abbrev-ref", "HEAD");
        return result.ExitCode == 0 ? result.Output.Trim() : string.Empty;
    }

    private static async Task<string> GetMainBranchName(string directory)
    {
        // Try 'main' first
        var result = await RunGitCommand(directory, "rev-parse", "--verify", "main");
        if (result.ExitCode == 0)
            return "main";

        // Try 'master'
        result = await RunGitCommand(directory, "rev-parse", "--verify", "master");
        if (result.ExitCode == 0)
            return "master";

        // Try 'origin/main'
        result = await RunGitCommand(directory, "rev-parse", "--verify", "origin/main");
        if (result.ExitCode == 0)
            return "origin/main";

        // Try 'origin/master'
        result = await RunGitCommand(directory, "rev-parse", "--verify", "origin/master");
        if (result.ExitCode == 0)
            return "origin/master";

        return string.Empty;
    }

    private static async Task<string> GetDiff(string directory, string fromBranch, string toBranch)
    {
        // Use git diff to compare branches
        // Shows what changes would be introduced by merging toBranch into fromBranch
        var result = await RunGitCommand(directory, "diff", $"{fromBranch}...{toBranch}");
        return result.Output;
    }

    private static void PrintColorizedDiff(string diff)
    {
        var lines = diff.Split('\n');

        foreach (var line in lines)
        {
            if (line.StartsWith("+++") || line.StartsWith("---"))
            {
                Console.ForegroundColor = ConsoleColor.White;
            }
            else if (line.StartsWith('+'))
            {
                Console.ForegroundColor = ConsoleColor.Green;
            }
            else if (line.StartsWith('-'))
            {
                Console.ForegroundColor = ConsoleColor.Red;
            }
            else if (line.StartsWith("@@"))
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
            }
            else if (line.StartsWith("diff --git"))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
            }
            else
            {
                Console.ResetColor();
            }

            Console.WriteLine(line);
        }

        Console.ResetColor();
    }

    private static async Task<GitCommandResult> RunGitCommand(string workingDirectory, params string[] arguments)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "git",
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var arg in arguments)
        {
            processStartInfo.ArgumentList.Add(arg);
        }

        using var process = new Process { StartInfo = processStartInfo };

        try
        {
            process.Start();

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            return new GitCommandResult
            {
                ExitCode = process.ExitCode,
                Output = output,
                Error = error
            };
        }
        catch (Exception ex)
        {
            return new GitCommandResult
            {
                ExitCode = -1,
                Output = string.Empty,
                Error = ex.Message
            };
        }
    }

    private class GitCommandResult
    {
        public int ExitCode { get; init; }
        public string Output { get; init; } = string.Empty;
        public string Error { get; init; } = string.Empty;
    }
}
