// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.CommandLine;
using LibGit2Sharp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using GitAnalysis.Core.Interfaces;
using GitAnalysis.Infrastructure.Services;

var fromOption = new Option<string?>(
    aliases: new[] { "--from", "-f" },
    description: "The branch to compare from (default: current branch)")
{ IsRequired = false };

var intoOption = new Option<string>(
    aliases: new[] { "--into", "-i" },
    description: "The branch to compare into (default: main)",
    getDefaultValue: () => "main")
{ IsRequired = false };

var repositoryOption = new Option<string?>(
    aliases: new[] { "--repository", "-r" },
    description: "Path to the Git repository (default: current directory)")
{ IsRequired = false };

var verboseOption = new Option<bool>(
    aliases: new[] { "--verbose", "-v" },
    description: "Enable verbose logging output",
    getDefaultValue: () => false)
{ IsRequired = false };

var rootCommand = new RootCommand("Code Review Tool - Compare Git branches")
{
    fromOption,
    intoOption,
    repositoryOption,
    verboseOption
};

rootCommand.SetHandler(async (string? fromBranch, string intoBranch, string? repoPath, bool verbose) =>
{
    try
    {
        // Determine repository path
        var repositoryPath = repoPath ?? Directory.GetCurrentDirectory();
        
        if (!Repository.IsValid(repositoryPath))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: '{repositoryPath}' is not a valid Git repository.");
            Console.ResetColor();
            Environment.Exit(1);
        }

        // Determine from branch (default to current branch)
        string actualFromBranch;
        if (string.IsNullOrEmpty(fromBranch))
        {
            using var repo = new Repository(repositoryPath);
            actualFromBranch = repo.Head.FriendlyName;
            Console.WriteLine($"Using current branch as 'from' branch: {actualFromBranch}");
        }
        else
        {
            actualFromBranch = fromBranch;
        }

        // Setup DI
        var services = new ServiceCollection();
        var logLevel = verbose ? Microsoft.Extensions.Logging.LogLevel.Information : Microsoft.Extensions.Logging.LogLevel.Warning;
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(logLevel));
        services.AddSingleton<IGitIgnoreEngine, GitIgnoreEngine>();
        services.AddSingleton<IGitService, GitService>();
        
        var serviceProvider = services.BuildServiceProvider();
        var gitService = serviceProvider.GetRequiredService<IGitService>();

        // Display comparison info
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║           Code Review Tool - Branch Comparison               ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        Console.ResetColor();
        Console.WriteLine();
        Console.WriteLine($"Repository: {repositoryPath}");
        Console.WriteLine($"Comparing:  {actualFromBranch} → {intoBranch}");
        Console.WriteLine();

        // Verify branches exist
        if (!await gitService.BranchExistsAsync(repositoryPath, actualFromBranch))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: Branch '{actualFromBranch}' does not exist.");
            Console.ResetColor();
            Environment.Exit(1);
        }

        if (!await gitService.BranchExistsAsync(repositoryPath, intoBranch))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: Branch '{intoBranch}' does not exist.");
            Console.ResetColor();
            Environment.Exit(1);
        }

        // Generate diff
        Console.WriteLine("Analyzing differences...");
        Console.WriteLine();
        
        var result = await gitService.GenerateDiffAsync(repositoryPath, actualFromBranch, intoBranch);

        // Display results
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("  Comparison Results");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.ResetColor();
        Console.WriteLine();
        Console.WriteLine($"Files changed:     {result.FileDiffs.Count}");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Total additions:   +{result.TotalAdditions}");
        Console.ResetColor();
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Total deletions:   -{result.TotalDeletions}");
        Console.ResetColor();
        Console.WriteLine($"Total modified:    {result.TotalModifications}");
        Console.WriteLine();

        if (result.FileDiffs.Count > 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Changed Files:");
            Console.WriteLine("─────────────────────────────────────────────────────────────");
            Console.ResetColor();
            
            foreach (var file in result.FileDiffs)
            {
                var changeIcon = file.ChangeType switch
                {
                    GitAnalysis.Core.Entities.FileChangeType.Added => "+",
                    GitAnalysis.Core.Entities.FileChangeType.Deleted => "-",
                    GitAnalysis.Core.Entities.FileChangeType.Modified => "M",
                    _ => "?"
                };

                Console.Write($"  {changeIcon} {file.FilePath,-50}");
                
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write($" +{file.Additions,4}");
                Console.ResetColor();
                Console.Write(" ");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write($"-{file.Deletions,4}");
                Console.ResetColor();
                Console.WriteLine();
            }
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("No differences found between the branches.");
            Console.ResetColor();
        }

        Console.WriteLine();
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Error: {ex.Message}");
        Console.ResetColor();
        Environment.Exit(1);
    }
}, fromOption, intoOption, repositoryOption, verboseOption);

return await rootCommand.InvokeAsync(args);

// Make Program accessible to tests
public partial class Program { }

