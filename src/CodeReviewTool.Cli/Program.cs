// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.CommandLine;
using LibGit2Sharp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using GitAnalysis.Core.Interfaces;
using GitAnalysis.Infrastructure.Services;
using GitAnalysis.Core.Services.StaticAnalysis;
using GitAnalysis.Core.Services.StaticAnalysis.CSharp;
using GitAnalysis.Core.Services.StaticAnalysis.Angular;
using GitAnalysis.Core.Services.StaticAnalysis.Scss;
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
        services.AddSingleton<ICSharpStaticAnalysisService, CSharpStaticAnalysisService>();
        services.AddSingleton<IAngularStaticAnalysisService, AngularStaticAnalysisService>();
        services.AddSingleton<IScssStaticAnalysisService, ScssStaticAnalysisService>();
        
        var serviceProvider = services.BuildServiceProvider();
        var gitService = serviceProvider.GetRequiredService<IGitService>();
        var analysisService = serviceProvider.GetRequiredService<IStaticAnalysisService>();
        var csharpAnalysisService = serviceProvider.GetRequiredService<ICSharpStaticAnalysisService>();
        var angularAnalysisService = serviceProvider.GetRequiredService<IAngularStaticAnalysisService>();
        var scssAnalysisService = serviceProvider.GetRequiredService<IScssStaticAnalysisService>();

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
                    .Where(f => File.Exists(f) && (f.EndsWith(".cs") || f.EndsWith(".ts") || f.EndsWith(".js") || f.EndsWith(".html") || f.EndsWith(".scss")))
                    .ToList();
                
                if (changedFilesToAnalyze.Count == 0)
                {
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine("No analyzable files found (only .cs, .ts, .js, .html, and .scss files are supported).");
                    Console.ResetColor();
                }
                else
                {
                    Console.WriteLine($"Analyzing {changedFilesToAnalyze.Count} file(s)...");
                    Console.WriteLine();
                    
                    var allViolations = new List<GitAnalysis.Core.Services.StaticAnalysis.Models.AnalysisViolation>();
                    var allWarnings = new List<GitAnalysis.Core.Services.StaticAnalysis.Models.AnalysisWarning>();
                    
                    try
                    {
                        // Run general static analysis (for copyright headers, message design, etc.)
                        var projectAnalysis = await analysisService.AnalyzeAsync(changedFilesToAnalyze.First());
                        
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
                        
                        allViolations.AddRange(relevantViolations);
                        allWarnings.AddRange(relevantWarnings);
                        
                        // Run C# specific analysis on .cs files
                        var csFiles = changedFilesToAnalyze.Where(f => f.EndsWith(".cs")).ToList();
                        if (csFiles.Count > 0)
                        {
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.WriteLine($"Running C# analysis on {csFiles.Count} file(s)...");
                            Console.ResetColor();
                            
                            var csharpResult = await csharpAnalysisService.AnalyzeAsync(repositoryPath);
                            
                            // Convert C# analysis results to our common format
                            foreach (var issue in csharpResult.Issues.Where(i => csFiles.Any(f => f.EndsWith(i.FilePath, StringComparison.OrdinalIgnoreCase))))
                            {
                                if (issue.Severity == GitAnalysis.Core.Services.StaticAnalysis.Models.IssueSeverity.Error)
                                {
                                    allViolations.Add(new GitAnalysis.Core.Services.StaticAnalysis.Models.AnalysisViolation
                                    {
                                        RuleId = issue.RuleId,
                                        SpecSource = "roslyn-analysis",
                                        Severity = issue.Severity,
                                        Message = issue.Message,
                                        FilePath = issue.FilePath,
                                        LineNumber = issue.Line,
                                        SuggestedFix = issue.SuggestedFix
                                    });
                                }
                                else
                                {
                                    allWarnings.Add(new GitAnalysis.Core.Services.StaticAnalysis.Models.AnalysisWarning
                                    {
                                        RuleId = issue.RuleId,
                                        SpecSource = "roslyn-analysis",
                                        Message = issue.Message,
                                        FilePath = issue.FilePath,
                                        LineNumber = issue.Line,
                                        Recommendation = issue.SuggestedFix
                                    });
                                }
                            }
                        }
                        
                        // Run Angular specific analysis if angular.json exists
                        var angularRoot = angularAnalysisService.FindWorkspaceRoot(repositoryPath);
                        if (angularRoot != null)
                        {
                            var tsFiles = changedFilesToAnalyze.Where(f => f.EndsWith(".ts") || f.EndsWith(".html")).ToList();
                            if (tsFiles.Count > 0)
                            {
                                Console.ForegroundColor = ConsoleColor.Cyan;
                                Console.WriteLine($"Running Angular analysis on {tsFiles.Count} file(s)...");
                                Console.ResetColor();
                                
                                var angularResult = await angularAnalysisService.AnalyzeAsync(angularRoot);
                                
                                // Convert Angular analysis results
                                foreach (var issue in angularResult.Issues.Where(i => !string.IsNullOrEmpty(i.FilePath) && tsFiles.Any(f => f.Contains(i.FilePath, StringComparison.OrdinalIgnoreCase))))
                                {
                                    if (issue.Severity == GitAnalysis.Core.Services.StaticAnalysis.Models.IssueSeverity.Error)
                                    {
                                        allViolations.Add(new GitAnalysis.Core.Services.StaticAnalysis.Models.AnalysisViolation
                                        {
                                            RuleId = issue.Category,
                                            SpecSource = "angular-analysis",
                                            Severity = issue.Severity,
                                            Message = issue.Message,
                                            FilePath = issue.FilePath,
                                            LineNumber = issue.Line,
                                            SuggestedFix = issue.Suggestion
                                        });
                                    }
                                    else
                                    {
                                        allWarnings.Add(new GitAnalysis.Core.Services.StaticAnalysis.Models.AnalysisWarning
                                        {
                                            RuleId = issue.Category,
                                            SpecSource = "angular-analysis",
                                            Message = issue.Message,
                                            FilePath = issue.FilePath,
                                            LineNumber = issue.Line,
                                            Recommendation = issue.Suggestion
                                        });
                                    }
                                }
                            }
                        }
                        
                        // Run SCSS specific analysis on .scss files
                        var scssFiles = changedFilesToAnalyze.Where(f => f.EndsWith(".scss")).ToList();
                        if (scssFiles.Count > 0)
                        {
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.WriteLine($"Running SCSS analysis on {scssFiles.Count} file(s)...");
                            Console.ResetColor();
                            
                            foreach (var scssFile in scssFiles)
                            {
                                var scssResult = await scssAnalysisService.AnalyzeFileAsync(scssFile);
                                
                                // Convert SCSS issues
                                foreach (var issue in scssResult.Issues)
                                {
                                    if (issue.Severity == GitAnalysis.Core.Services.StaticAnalysis.Models.IssueSeverity.Error)
                                    {
                                        allViolations.Add(new GitAnalysis.Core.Services.StaticAnalysis.Models.AnalysisViolation
                                        {
                                            RuleId = issue.Code,
                                            SpecSource = $"scss-analysis ({issue.Rule})",
                                            Severity = issue.Severity,
                                            Message = issue.Message,
                                            FilePath = Path.GetFileName(scssFile),
                                            LineNumber = issue.Line,
                                            SuggestedFix = issue.SourceSnippet
                                        });
                                    }
                                    else
                                    {
                                        allWarnings.Add(new GitAnalysis.Core.Services.StaticAnalysis.Models.AnalysisWarning
                                        {
                                            RuleId = issue.Code,
                                            SpecSource = $"scss-analysis ({issue.Rule})",
                                            Message = issue.Message,
                                            FilePath = Path.GetFileName(scssFile),
                                            LineNumber = issue.Line,
                                            Recommendation = issue.SourceSnippet
                                        });
                                    }
                                }
                            }
                        }
                        
                        Console.WriteLine();
                        
                        // Display results
                        if (allViolations.Count == 0 && allWarnings.Count == 0)
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("✓ No issues found in changed files!");
                            Console.ResetColor();
                        }
                        else
                        {
                            if (allViolations.Count > 0)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine($"Found {allViolations.Count} violation(s):");
                                Console.WriteLine();
                                Console.ResetColor();
                                
                                foreach (var violation in allViolations)
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
                            
                            if (allWarnings.Count > 0)
                            {
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.WriteLine($"Found {allWarnings.Count} warning(s):");
                                Console.WriteLine();
                                Console.ResetColor();
                                
                                foreach (var warning in allWarnings)
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

