// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Text.RegularExpressions;
using GitAnalysis.Core.Services.StaticAnalysis.Models;
using Microsoft.Extensions.Logging;

namespace GitAnalysis.Core.Services.StaticAnalysis;

/// <summary>
/// Service for performing static analysis on code repositories based on specification files.
/// </summary>
public class StaticAnalysisService : IStaticAnalysisService
{
    private readonly ILogger<StaticAnalysisService> _logger;

    public StaticAnalysisService(ILogger<StaticAnalysisService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<AnalysisResult> AnalyzeAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var projectRoot = DetermineProjectRoot(filePath);

        if (projectRoot == null)
        {
            throw new InvalidOperationException(
                $"Unable to determine project root from path '{filePath}'. " +
                "The path must be within a Git repository, .NET solution, Angular workspace, or Node environment.");
        }

        var (rootDirectory, projectType) = projectRoot.Value;

        _logger.LogInformation("Analyzing {ProjectType} at: {RootDirectory}", projectType, rootDirectory);

        var result = new AnalysisResult
        {
            RootDirectory = rootDirectory,
            ProjectType = projectType
        };

        // Only analyze C# files for .NET projects
        if (projectType == ProjectType.GitRepository || projectType == ProjectType.DotNetSolution)
        {
            var gitignorePatterns = LoadGitignorePatterns(rootDirectory);
            
            var csFiles = Directory.GetFiles(rootDirectory, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar) &&
                           !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar) &&
                           !ShouldIgnoreFile(f, rootDirectory, gitignorePatterns))
                .ToList();

            result.TotalFilesAnalyzed = csFiles.Count;

            foreach (var csFile in csFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await AnalyzeCSharpFileAsync(csFile, result, cancellationToken);
            }
        }

        _logger.LogInformation(
            "Analysis complete. Files: {FileCount}, Violations: {ViolationCount}, Warnings: {WarningCount}",
            result.TotalFilesAnalyzed, result.Violations.Count, result.Warnings.Count);

        return result;
    }

    /// <inheritdoc/>
    public (string RootDirectory, ProjectType ProjectType)? DetermineProjectRoot(string filePath)
    {
        var directory = File.Exists(filePath) ? Path.GetDirectoryName(filePath) : filePath;

        if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
        {
            directory = Environment.CurrentDirectory;
        }

        // Walk up the directory tree looking for project indicators
        var current = new DirectoryInfo(directory);

        while (current != null)
        {
            // Check for Git repository
            if (Directory.Exists(Path.Combine(current.FullName, ".git")))
            {
                return (current.FullName, ProjectType.GitRepository);
            }

            // Check for .NET Solution
            if (current.GetFiles("*.sln").Length > 0)
            {
                return (current.FullName, ProjectType.DotNetSolution);
            }

            // Check for Angular workspace
            if (File.Exists(Path.Combine(current.FullName, "angular.json")))
            {
                return (current.FullName, ProjectType.AngularWorkspace);
            }

            // Check for Node environment
            if (File.Exists(Path.Combine(current.FullName, "package.json")))
            {
                return (current.FullName, ProjectType.NodeEnvironment);
            }

            current = current.Parent;
        }

        return null;
    }

    private async Task AnalyzeCSharpFileAsync(string filePath, AnalysisResult result, CancellationToken cancellationToken)
    {
        var content = await File.ReadAllTextAsync(filePath, cancellationToken);
        var lines = content.Split('\n');
        var relativePath = Path.GetRelativePath(result.RootDirectory, filePath);

        // Check for copyright header (implementation.spec.md AC5.1)
        AnalyzeCopyrightHeader(filePath, relativePath, lines, result);

        // Check for MessagePack message design rules (message-design.spec.md)
        AnalyzeMessageDesignRules(filePath, relativePath, content, lines, result);

        // Check for subscription design rules (subscription-design.spec.md)
        AnalyzeSubscriptionDesignRules(filePath, relativePath, content, lines, result);

        // Check for implementation spec rules (implementation.spec.md)
        AnalyzeImplementationRules(filePath, relativePath, content, lines, result);
    }

    private void AnalyzeCopyrightHeader(string filePath, string relativePath, string[] lines, AnalysisResult result)
    {
        var expectedLine1 = "// Copyright (c) Quinntyne Brown. All Rights Reserved.";
        var expectedLine2 = "// Licensed under the MIT License. See License.txt in the project root for license information.";

        if (lines.Length < 2 ||
            !lines[0].Trim().Equals(expectedLine1, StringComparison.Ordinal) ||
            !lines[1].Trim().Equals(expectedLine2, StringComparison.Ordinal))
        {
            result.Violations.Add(new AnalysisViolation
            {
                RuleId = "AC5.1",
                SpecSource = "implementation.spec.md",
                Severity = IssueSeverity.Error,
                Message = "Missing or incorrect copyright header.",
                FilePath = relativePath,
                LineNumber = 1,
                SuggestedFix = $"Add the following header at the top of the file:\n{expectedLine1}\n{expectedLine2}"
            });
        }
    }

    private void AnalyzeMessageDesignRules(string filePath, string relativePath, string content, string[] lines, AnalysisResult result)
    {
        // Skip non-message files
        if (!content.Contains("[MessagePackObject]"))
        {
            return;
        }

        result.Info.Add(new AnalysisInfo
        {
            Category = "MessagePack",
            Message = $"Found MessagePack message class.",
            FilePath = relativePath
        });

        // REQ-MSG-001: Check for MessageEnvelope pattern
        if (content.Contains("class") && content.Contains("[MessagePackObject]") &&
            !content.Contains("MessageEnvelope") && !content.Contains("IMessage"))
        {
            result.Warnings.Add(new AnalysisWarning
            {
                RuleId = "REQ-MSG-001",
                SpecSource = "message-design.spec.md",
                Message = "Message class should implement IMessage marker interface.",
                FilePath = relativePath,
                Recommendation = "Ensure message classes implement IMessage, IDomainEvent, or ICommand interfaces."
            });
        }

        // REQ-MSG-002: Check for required header fields in MessageHeader classes
        if (content.Contains("class MessageHeader") || content.Contains("class MessageEnvelope"))
        {
            var requiredFields = new[] { "MessageType", "MessageId", "CorrelationId", "CausationId", "TimestampUnixMs", "SourceService", "SchemaVersion" };
            foreach (var field in requiredFields)
            {
                if (!content.Contains(field))
                {
                    result.Violations.Add(new AnalysisViolation
                    {
                        RuleId = "REQ-MSG-002",
                        SpecSource = "message-design.spec.md",
                        Severity = IssueSeverity.Error,
                        Message = $"MessageHeader missing required field: {field}",
                        FilePath = relativePath,
                        SuggestedFix = $"Add the '{field}' property to the MessageHeader class."
                    });
                }
            }
        }

        // REQ-MSG-005: Check for MessagePack attributes
        if (content.Contains("[MessagePackObject]"))
        {
            // Check that properties have [Key] attributes
            var propertyPattern = new Regex(@"public\s+(?:required\s+)?[\w<>\[\],\s]+\s+(\w+)\s*{\s*get;", RegexOptions.Compiled);
            var keyPattern = new Regex(@"\[Key\(\d+\)\]", RegexOptions.Compiled);
            var ignoreMemberPattern = new Regex(@"\[IgnoreMember\]", RegexOptions.Compiled);

            var matches = propertyPattern.Matches(content);
            foreach (Match match in matches)
            {
                var propertyIndex = match.Index;
                var contextStart = Math.Max(0, propertyIndex - 100);
                var context = content.Substring(contextStart, Math.Min(200, content.Length - contextStart));

                if (!keyPattern.IsMatch(context) && !ignoreMemberPattern.IsMatch(context))
                {
                    var lineNumber = content.Take(propertyIndex).Count(c => c == '\n') + 1;
                    result.Warnings.Add(new AnalysisWarning
                    {
                        RuleId = "AC-MSG-005.2",
                        SpecSource = "message-design.spec.md",
                        Message = $"Property '{match.Groups[1].Value}' in MessagePackObject should have [Key(n)] or [IgnoreMember] attribute.",
                        FilePath = relativePath,
                        LineNumber = lineNumber,
                        Recommendation = "Add [Key(n)] attribute with unique integer key, or [IgnoreMember] if not serialized."
                    });
                }
            }
        }

        // REQ-MSG-007: Check channel naming pattern
        if (content.Contains("[MessageChannel"))
        {
            var channelPattern = new Regex(@"\[MessageChannel\s*\(\s*""([^""]+)""\s*,\s*""([^""]+)""\s*,\s*""([^""]+)""", RegexOptions.Compiled);
            var match = channelPattern.Match(content);
            if (match.Success)
            {
                var domain = match.Groups[1].Value;
                var aggregate = match.Groups[2].Value;
                var eventType = match.Groups[3].Value;

                if (domain != domain.ToLowerInvariant())
                {
                    result.Violations.Add(new AnalysisViolation
                    {
                        RuleId = "AC-MSG-007.2",
                        SpecSource = "message-design.spec.md",
                        Severity = IssueSeverity.Error,
                        Message = $"Domain in MessageChannel must be lowercase: '{domain}'",
                        FilePath = relativePath,
                        SuggestedFix = $"Change domain to '{domain.ToLowerInvariant()}'"
                    });
                }

                if (aggregate != aggregate.ToLowerInvariant())
                {
                    result.Violations.Add(new AnalysisViolation
                    {
                        RuleId = "AC-MSG-007.3",
                        SpecSource = "message-design.spec.md",
                        Severity = IssueSeverity.Error,
                        Message = $"Aggregate in MessageChannel must be lowercase: '{aggregate}'",
                        FilePath = relativePath,
                        SuggestedFix = $"Change aggregate to '{aggregate.ToLowerInvariant()}'"
                    });
                }

                if (eventType != eventType.ToLowerInvariant())
                {
                    result.Violations.Add(new AnalysisViolation
                    {
                        RuleId = "AC-MSG-007.4",
                        SpecSource = "message-design.spec.md",
                        Severity = IssueSeverity.Error,
                        Message = $"Event type in MessageChannel must be lowercase: '{eventType}'",
                        FilePath = relativePath,
                        SuggestedFix = $"Change event type to '{eventType.ToLowerInvariant()}'"
                    });
                }
            }
        }
    }

    private void AnalyzeSubscriptionDesignRules(string filePath, string relativePath, string content, string[] lines, AnalysisResult result)
    {
        // REQ-SUB-004: Check message handler implementation
        if (content.Contains("IMessageHandler<") || content.Contains(": MessageHandlerBase<"))
        {
            result.Info.Add(new AnalysisInfo
            {
                Category = "MessageHandler",
                Message = "Found message handler implementation.",
                FilePath = relativePath
            });

            // Check for HandleAsync method signature
            if (!content.Contains("HandleAsync"))
            {
                result.Violations.Add(new AnalysisViolation
                {
                    RuleId = "AC-SUB-004.1",
                    SpecSource = "subscription-design.spec.md",
                    Severity = IssueSeverity.Error,
                    Message = "Message handler must implement HandleAsync method.",
                    FilePath = relativePath,
                    SuggestedFix = "Implement HandleAsync(TMessage, MessageContext, CancellationToken) method."
                });
            }

            // Check for CancellationToken parameter
            if (content.Contains("HandleAsync") && !content.Contains("CancellationToken"))
            {
                result.Warnings.Add(new AnalysisWarning
                {
                    RuleId = "AC5.4",
                    SpecSource = "implementation.spec.md",
                    Message = "HandleAsync should accept CancellationToken parameter.",
                    FilePath = relativePath,
                    Recommendation = "Add CancellationToken parameter to HandleAsync method."
                });
            }
        }

        // REQ-SUB-009: Check for idempotency in handlers
        if ((content.Contains("IMessageHandler<") || content.Contains(": MessageHandlerBase<")) &&
            !content.Contains("IIdempotencyStore") && !content.Contains("HasProcessed") && !content.Contains("TryProcess"))
        {
            result.Warnings.Add(new AnalysisWarning
            {
                RuleId = "REQ-SUB-009",
                SpecSource = "subscription-design.spec.md",
                Message = "Message handler should implement idempotency checks.",
                FilePath = relativePath,
                Recommendation = "Inject IIdempotencyStore and check for duplicate processing using TryProcessAsync."
            });
        }
    }

    private void AnalyzeImplementationRules(string filePath, string relativePath, string content, string[] lines, AnalysisResult result)
    {
        // AC1.1/AC1.4: Check for monolithic strategies
        if (content.Contains("SyntaxGenerationStrategy") || content.Contains("ArtifactGenerationStrategy"))
        {
            var lineCount = lines.Length;
            if (lineCount > 300)
            {
                result.Warnings.Add(new AnalysisWarning
                {
                    RuleId = "AC1.4",
                    SpecSource = "implementation.spec.md",
                    Message = $"Strategy file has {lineCount} lines. Consider breaking into smaller, focused strategies.",
                    FilePath = relativePath,
                    Recommendation = "Strategies should be focused and compose other strategies via ISyntaxGenerator/IArtifactGenerator."
                });
            }
        }

        // AC2.1: Check that syntax strategies don't do file I/O
        if (content.Contains("SyntaxGenerationStrategy") &&
            (content.Contains("File.WriteAllText") || content.Contains("File.Create") ||
             content.Contains("Directory.Create") || content.Contains("IFileSystem")))
        {
            result.Violations.Add(new AnalysisViolation
            {
                RuleId = "AC2.1",
                SpecSource = "implementation.spec.md",
                Severity = IssueSeverity.Error,
                Message = "Syntax generation strategies must NOT perform file I/O.",
                FilePath = relativePath,
                SuggestedFix = "Move file I/O operations to an artifact generation strategy."
            });
        }

        // AC3.1: Check naming conventions for syntax strategies
        if (content.Contains("class") && content.Contains("SyntaxGenerationStrategy"))
        {
            var classNamePattern = new Regex(@"class\s+(\w+)\s*:", RegexOptions.Compiled);
            var match = classNamePattern.Match(content);
            if (match.Success)
            {
                var className = match.Groups[1].Value;
                if (!className.EndsWith("SyntaxGenerationStrategy"))
                {
                    result.Violations.Add(new AnalysisViolation
                    {
                        RuleId = "AC3.1",
                        SpecSource = "implementation.spec.md",
                        Severity = IssueSeverity.Error,
                        Message = $"Syntax generation strategy class must end with 'SyntaxGenerationStrategy': {className}",
                        FilePath = relativePath,
                        SuggestedFix = $"Rename class to end with 'SyntaxGenerationStrategy'."
                    });
                }
            }
        }

        // AC3.2: Check naming conventions for artifact strategies
        if (content.Contains("class") && content.Contains("ArtifactGenerationStrategy"))
        {
            var classNamePattern = new Regex(@"class\s+(\w+)\s*:", RegexOptions.Compiled);
            var match = classNamePattern.Match(content);
            if (match.Success)
            {
                var className = match.Groups[1].Value;
                if (!className.EndsWith("ArtifactGenerationStrategy") && !className.EndsWith("GenerationStrategy"))
                {
                    result.Violations.Add(new AnalysisViolation
                    {
                        RuleId = "AC3.2",
                        SpecSource = "implementation.spec.md",
                        Severity = IssueSeverity.Error,
                        Message = $"Artifact generation strategy class must end with 'ArtifactGenerationStrategy' or 'GenerationStrategy': {className}",
                        FilePath = relativePath,
                        SuggestedFix = $"Rename class to end with 'ArtifactGenerationStrategy' or 'GenerationStrategy'."
                    });
                }
            }
        }

        // AC3.3: Check Model naming conventions
        if (content.Contains("class") && (content.Contains(": SyntaxModel") || content.Contains(": ArtifactModel")))
        {
            var classNamePattern = new Regex(@"class\s+(\w+)\s*:", RegexOptions.Compiled);
            var match = classNamePattern.Match(content);
            if (match.Success)
            {
                var className = match.Groups[1].Value;
                if (!className.EndsWith("Model"))
                {
                    result.Violations.Add(new AnalysisViolation
                    {
                        RuleId = "AC3.3",
                        SpecSource = "implementation.spec.md",
                        Severity = IssueSeverity.Error,
                        Message = $"Model class must end with 'Model': {className}",
                        FilePath = relativePath,
                        SuggestedFix = $"Rename class to end with 'Model'."
                    });
                }
            }
        }

        // AC5.2: Check for dependency injection pattern
        if ((content.Contains("SyntaxGenerationStrategy") || content.Contains("ArtifactGenerationStrategy")) &&
            content.Contains("new SyntaxGenerator()"))
        {
            result.Violations.Add(new AnalysisViolation
            {
                RuleId = "AC5.2",
                SpecSource = "implementation.spec.md",
                Severity = IssueSeverity.Error,
                Message = "Strategies must use dependency injection, not direct instantiation.",
                FilePath = relativePath,
                SuggestedFix = "Inject ISyntaxGenerator via constructor instead of using 'new SyntaxGenerator()'."
            });
        }

        // AC5.3: Check for CanHandle and GetPriority implementation
        if (content.Contains("ISyntaxGenerationStrategy") || content.Contains("IArtifactGenerationStrategy"))
        {
            if (!content.Contains("CanHandle"))
            {
                result.Warnings.Add(new AnalysisWarning
                {
                    RuleId = "AC5.3",
                    SpecSource = "implementation.spec.md",
                    Message = "Strategy should implement CanHandle method.",
                    FilePath = relativePath,
                    Recommendation = "Implement CanHandle(object target) method."
                });
            }

            if (!content.Contains("GetPriority"))
            {
                result.Warnings.Add(new AnalysisWarning
                {
                    RuleId = "AC5.3",
                    SpecSource = "implementation.spec.md",
                    Message = "Strategy should implement GetPriority method.",
                    FilePath = relativePath,
                    Recommendation = "Implement GetPriority() method returning an integer priority."
                });
            }
        }
    }

    private List<string> LoadGitignorePatterns(string rootDirectory)
    {
        var patterns = new List<string>();
        var gitignorePath = Path.Combine(rootDirectory, ".gitignore");

        if (File.Exists(gitignorePath))
        {
            var lines = File.ReadAllLines(gitignorePath);
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                // Skip empty lines and comments
                if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#"))
                {
                    continue;
                }
                patterns.Add(trimmed);
            }
        }

        return patterns;
    }

    private bool ShouldIgnoreFile(string filePath, string rootDirectory, List<string> gitignorePatterns)
    {
        var relativePath = Path.GetRelativePath(rootDirectory, filePath)
            .Replace(Path.DirectorySeparatorChar, '/');

        foreach (var pattern in gitignorePatterns)
        {
            if (MatchesGitignorePattern(relativePath, pattern))
            {
                return true;
            }
        }

        return false;
    }

    private bool MatchesGitignorePattern(string path, string pattern)
    {
        // Remove leading slash
        if (pattern.StartsWith("/"))
        {
            pattern = pattern.Substring(1);
        }

        // Handle directory patterns (ending with /)
        if (pattern.EndsWith("/"))
        {
            var dirPattern = pattern.TrimEnd('/');
            
            // Check if the pattern appears anywhere in the path as a directory
            // e.g., "node_modules/" should match "src/app/node_modules/file.js"
            return path.Contains("/" + dirPattern + "/", StringComparison.OrdinalIgnoreCase) ||
                   path.StartsWith(dirPattern + "/", StringComparison.OrdinalIgnoreCase) ||
                   path.Equals(dirPattern, StringComparison.OrdinalIgnoreCase);
        }

        // Handle wildcard patterns
        if (pattern.Contains("*"))
        {
            // Convert gitignore pattern to regex
            var regexPattern = "^" + Regex.Escape(pattern)
                .Replace("\\*\\*/", "(.*/)?")  // **/ matches zero or more directories
                .Replace("\\*\\*", ".*")        // ** matches anything
                .Replace("\\*", "[^/]*")        // * matches anything except /
                .Replace("\\?", ".")            // ? matches single character
                + "$";

            return Regex.IsMatch(path, regexPattern, RegexOptions.IgnoreCase);
        }

        // Exact match or directory prefix match
        return path.Equals(pattern, StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith(pattern + "/", StringComparison.OrdinalIgnoreCase) ||
               path.Contains("/" + pattern + "/", StringComparison.OrdinalIgnoreCase);
    }
}
