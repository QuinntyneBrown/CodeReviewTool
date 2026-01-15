// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.CommandLine;
using LibGit2Sharp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using GitAnalysis.Core.Interfaces;
using GitAnalysis.Infrastructure.Services;
using GitAnalysis.Core.Services.StaticAnalysis;
using GitAnalysis.Core.Entities;

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

var showDiffOption = new Option<bool>(
    aliases: new[] { "--show-diff", "-d" },
    description: "Show line-by-line diff for changed files",
    getDefaultValue: () => false)
{ IsRequired = false };

var analyzeOption = new Option<bool>(
    aliases: new[] { "--analyze", "-a" },
    description: "Run static analysis on changed files",
    getDefaultValue: () => true)
{ IsRequired = false };

var rootCommand = new RootCommand("Code Review Tool - Compare Git branches")
{
    fromOption,
    intoOption,
    repositoryOption,
    verboseOption,
    showDiffOption,
    analyzeOption
};

rootCommand.SetHandler(async (string? fromBranch, string intoBranch, string? repoPath, bool verbose, bool showDiff, bool analyze) =>
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
        services.AddSingleton<IStaticAnalysisService, StaticAnalysisService>();
        
        var serviceProvider = services.BuildServiceProvider();
        var gitService = serviceProvider.GetRequiredService<IGitService>();
        var analysisService = serviceProvider.GetRequiredService<IStaticAnalysisService>();

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
            
            // Show line-by-line diffs if requested
            if (showDiff)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("═══════════════════════════════════════════════════════════════");
                Console.WriteLine("  Line-by-Line Diffs");
                Console.WriteLine("═══════════════════════════════════════════════════════════════");
                Console.ResetColor();
                Console.WriteLine();
                
                foreach (var file in result.FileDiffs)
                {
                    if (file.LineChanges.Count == 0)
                        continue;
                        
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"──── {file.FilePath} ────");
                    Console.ResetColor();
                    
                    foreach (var line in file.LineChanges)
                    {
                        switch (line.Type)
                        {
                            case DiffType.Addition:
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine($"  +{line.LineNumber,4} | {line.Content}");
                                break;
                            case DiffType.Deletion:
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine($"  -{line.LineNumber,4} | {line.Content}");
                                break;
                            case DiffType.Context:
                                Console.ForegroundColor = ConsoleColor.Gray;
                                Console.WriteLine($"   {line.LineNumber,4} | {line.Content}");
                                break;
                        }
                        Console.ResetColor();
                    }
                    Console.WriteLine();
                }
            }
            
            // Run static analysis on changed files if requested
            if (analyze)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("═══════════════════════════════════════════════════════════════");
                Console.WriteLine("  Static Analysis Results");
                Console.WriteLine("═══════════════════════════════════════════════════════════════");
                Console.ResetColor();
                Console.WriteLine();
                Console.WriteLine("Analyzing changed files...");
                Console.WriteLine();
                
                var analysisResults = new Dictionary<string, GitAnalysis.Core.Services.StaticAnalysis.Models.AnalysisResult>();
                var changedFilesToAnalyze = result.FileDiffs
                    .Where(f => f.ChangeType != FileChangeType.Deleted)
                    .Select(f => Path.Combine(repositoryPath, f.FilePath))
                    .Where(f => File.Exists(f) && (f.EndsWith(".cs") || f.EndsWith(".ts") || f.EndsWith(".js")))
                    .ToList();
                
                if (changedFilesToAnalyze.Count == 0)
                {
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine("No analyzable files found (only .cs, .ts, and .js files are supported).");
                    Console.ResetColor();
                }
                else
                {
                    Console.WriteLine($"Analyzing {changedFilesToAnalyze.Count} file(s)...");
                    Console.WriteLine();
                    
                    // For each changed file, run analysis on the entire project/repository
                    // The analysis service will analyze the whole context and we'll filter for changed files
                    GitAnalysis.Core.Services.StaticAnalysis.Models.AnalysisResult? projectAnalysis = null;
                    
                    try
                    {
                        // Analyze from the first changed file (the service will find the project root)
                        projectAnalysis = await analysisService.AnalyzeAsync(changedFilesToAnalyze.First());
                        
                        // Filter violations for only changed files
                        var changedFileNames = new HashSet<string>(
                            changedFilesToAnalyze.Select(f => Path.GetFileName(f)),
                            StringComparer.OrdinalIgnoreCase
                        );
                        
                        var relevantViolations = projectAnalysis.Violations
                            .Where(v => v.FilePath != null && changedFileNames.Contains(Path.GetFileName(v.FilePath)))
                            .ToList();
                        
                        var relevantWarnings = projectAnalysis.Warnings
                            .Where(w => w.FilePath != null && changedFileNames.Contains(Path.GetFileName(w.FilePath)))
                            .ToList();
                        
                        // Display results
                        if (relevantViolations.Count == 0 && relevantWarnings.Count == 0)
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("✓ No issues found in changed files!");
                            Console.ResetColor();
                        }
                        else
                        {
                            if (relevantViolations.Count > 0)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine($"Found {relevantViolations.Count} violation(s):");
                                Console.WriteLine();
                                Console.ResetColor();
                                
                                foreach (var violation in relevantViolations)
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.Write("  ✗ ");
                                    Console.ResetColor();
                                    Console.WriteLine($"[{violation.RuleId}] {violation.Message}");
                                    
                                    if (violation.FilePath != null)
                                    {
                                        Console.ForegroundColor = ConsoleColor.Gray;
                                        var location = violation.LineNumber.HasValue 
                                            ? $"{Path.GetFileName(violation.FilePath)}:{violation.LineNumber}"
                                            : Path.GetFileName(violation.FilePath);
                                        Console.WriteLine($"    Location: {location}");
                                        Console.WriteLine($"    Source: {violation.SpecSource}");
                                        
                                        if (!string.IsNullOrEmpty(violation.SuggestedFix))
                                        {
                                            Console.WriteLine($"    Fix: {violation.SuggestedFix}");
                                        }
                                        Console.ResetColor();
                                    }
                                    Console.WriteLine();
                                }
                            }
                            
                            if (relevantWarnings.Count > 0)
                            {
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.WriteLine($"Found {relevantWarnings.Count} warning(s):");
                                Console.WriteLine();
                                Console.ResetColor();
                                
                                foreach (var warning in relevantWarnings)
                                {
                                    Console.ForegroundColor = ConsoleColor.Yellow;
                                    Console.Write("  ⚠ ");
                                    Console.ResetColor();
                                    Console.WriteLine($"[{warning.RuleId}] {warning.Message}");
                                    
                                    if (warning.FilePath != null)
                                    {
                                        Console.ForegroundColor = ConsoleColor.Gray;
                                        var location = warning.LineNumber.HasValue 
                                            ? $"{Path.GetFileName(warning.FilePath)}:{warning.LineNumber}"
                                            : Path.GetFileName(warning.FilePath);
                                        Console.WriteLine($"    Location: {location}");
                                        Console.WriteLine($"    Source: {warning.SpecSource}");
                                        Console.ResetColor();
                                    }
                                    Console.WriteLine();
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"⚠ Static analysis failed: {ex.Message}");
                        Console.ResetColor();
                    }
                }
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
}, fromOption, intoOption, repositoryOption, verboseOption, showDiffOption, analyzeOption);

return await rootCommand.InvokeAsync(args);

// Make Program accessible to tests
public partial class Program { }

