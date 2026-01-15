// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Diagnostics;
using System.Text.RegularExpressions;
using GitAnalysis.Core.Services.StaticAnalysis.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;

namespace GitAnalysis.Core.Services.StaticAnalysis.CSharp;

/// <summary>
/// Service for performing static analysis on C# code using Roslyn.
/// </summary>
public class CSharpStaticAnalysisService : ICSharpStaticAnalysisService
{
    private readonly ILogger<CSharpStaticAnalysisService> _logger;

    public CSharpStaticAnalysisService(ILogger<CSharpStaticAnalysisService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<CSharpStaticAnalysisResult> AnalyzeAsync(
        string path,
        CSharpStaticAnalysisOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= CSharpStaticAnalysisOptions.Default;
        var stopwatch = Stopwatch.StartNew();
        var result = new CSharpStaticAnalysisResult();

        try
        {
            var (rootPath, targetType) = FindRoot(path);
            result.RootPath = rootPath;
            result.TargetType = targetType;

            _logger.LogInformation("Starting C# static analysis on {TargetType}: {RootPath}", targetType, rootPath);

            var csFiles = GetCSharpFiles(rootPath, options);
            _logger.LogInformation("Found {FileCount} C# files to analyze", csFiles.Count);

            foreach (var filePath in csFiles)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                await AnalyzeFileAsync(filePath, result, options, cancellationToken);

                if (options.MaxIssues > 0 && result.Issues.Count >= options.MaxIssues)
                {
                    _logger.LogWarning("Reached maximum issue limit of {MaxIssues}", options.MaxIssues);
                    break;
                }
            }

            // Calculate summary
            CalculateSummary(result, stopwatch.ElapsedMilliseconds);
            result.Success = true;

            _logger.LogInformation("Analysis completed in {Duration}ms. Found {IssueCount} issues in {FileCount} files",
                stopwatch.ElapsedMilliseconds, result.Issues.Count, result.FileStats.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Static analysis failed");
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    /// <inheritdoc />
    public string DetermineTargetType(string path)
    {
        if (File.Exists(path))
        {
            if (path.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
                return "Solution";
            if (path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                return "Project";
            if (path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                return "File";
        }

        if (Directory.Exists(path))
            return "Directory";

        return "Unknown";
    }

    /// <inheritdoc />
    public (string RootPath, string TargetType) FindRoot(string path)
    {
        var fullPath = Path.GetFullPath(path);

        // If path is a file, use it directly
        if (File.Exists(fullPath))
        {
            return (fullPath, DetermineTargetType(fullPath));
        }

        // If path is a directory, search for solution or project files
        if (Directory.Exists(fullPath))
        {
            // First look for .sln file
            var slnFiles = Directory.GetFiles(fullPath, "*.sln", SearchOption.TopDirectoryOnly);
            if (slnFiles.Length > 0)
            {
                return (fullPath, "Solution");
            }

            // Then look for .csproj file
            var csprojFiles = Directory.GetFiles(fullPath, "*.csproj", SearchOption.TopDirectoryOnly);
            if (csprojFiles.Length > 0)
            {
                return (fullPath, "Project");
            }

            // Search parent directories for solution or project
            var currentDir = fullPath;
            while (!string.IsNullOrEmpty(currentDir))
            {
                var parentSlnFiles = Directory.GetFiles(currentDir, "*.sln", SearchOption.TopDirectoryOnly);
                if (parentSlnFiles.Length > 0)
                {
                    return (currentDir, "Solution");
                }

                var parentCsprojFiles = Directory.GetFiles(currentDir, "*.csproj", SearchOption.TopDirectoryOnly);
                if (parentCsprojFiles.Length > 0)
                {
                    return (currentDir, "Project");
                }

                var parent = Directory.GetParent(currentDir);
                currentDir = parent?.FullName;
            }

            // If no solution or project found, return the directory itself
            return (fullPath, "Directory");
        }

        throw new ArgumentException($"Path does not exist: {path}");
    }

    private List<string> GetCSharpFiles(string rootPath, CSharpStaticAnalysisOptions options)
    {
        var files = new List<string>();
        var targetType = DetermineTargetType(rootPath);

        if (targetType == "File" && rootPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
        {
            files.Add(rootPath);
        }
        else if (Directory.Exists(rootPath))
        {
            files.AddRange(Directory.GetFiles(rootPath, "*.cs", SearchOption.AllDirectories)
                .Where(f => !IsGeneratedFile(f) && (options.IncludeTests || !IsTestFile(f))));
        }

        return files;
    }

    private static bool IsGeneratedFile(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        return fileName.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase) ||
               fileName.EndsWith(".generated.cs", StringComparison.OrdinalIgnoreCase) ||
               fileName.EndsWith(".designer.cs", StringComparison.OrdinalIgnoreCase) ||
               filePath.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") ||
               filePath.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}");
    }

    private static bool IsTestFile(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        var pathLower = filePath.ToLowerInvariant();
        return fileName.EndsWith("Tests.cs", StringComparison.OrdinalIgnoreCase) ||
               fileName.EndsWith("Test.cs", StringComparison.OrdinalIgnoreCase) ||
               fileName.EndsWith(".Tests.cs", StringComparison.OrdinalIgnoreCase) ||
               pathLower.Contains($"{Path.DirectorySeparatorChar}tests{Path.DirectorySeparatorChar}") ||
               pathLower.Contains($"{Path.DirectorySeparatorChar}test{Path.DirectorySeparatorChar}");
    }

    private async Task AnalyzeFileAsync(
        string filePath,
        CSharpStaticAnalysisResult result,
        CSharpStaticAnalysisOptions options,
        CancellationToken cancellationToken)
    {
        try
        {
            var code = await File.ReadAllTextAsync(filePath, cancellationToken);
            var tree = CSharpSyntaxTree.ParseText(code, cancellationToken: cancellationToken);
            var root = await tree.GetRootAsync(cancellationToken);

            var fileStats = new FileAnalysisStats
            {
                FilePath = filePath,
                LinesOfCode = code.Split('\n').Length
            };

            // Collect type statistics
            var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>().ToList();
            var interfaces = root.DescendantNodes().OfType<InterfaceDeclarationSyntax>().ToList();
            var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>().ToList();
            var properties = root.DescendantNodes().OfType<PropertyDeclarationSyntax>().ToList();

            fileStats.ClassCount = classes.Count;
            fileStats.InterfaceCount = interfaces.Count;
            fileStats.MethodCount = methods.Count;
            fileStats.PropertyCount = properties.Count;

            // Run all analyzers
            var issues = new List<StaticAnalysisIssue>();

            if (ShouldAnalyzeCategory(IssueCategory.Naming, options))
                issues.AddRange(AnalyzeNamingConventions(filePath, root, code));

            if (ShouldAnalyzeCategory(IssueCategory.Style, options))
                issues.AddRange(AnalyzeCodeStyle(filePath, root, code));

            if (ShouldAnalyzeCategory(IssueCategory.CodeQuality, options))
                issues.AddRange(AnalyzeCodeQuality(filePath, root, code));

            if (ShouldAnalyzeCategory(IssueCategory.UnusedCode, options))
                issues.AddRange(AnalyzeUnusedCode(filePath, root, code));

            if (ShouldAnalyzeCategory(IssueCategory.Documentation, options))
                issues.AddRange(AnalyzeDocumentation(filePath, root, classes, interfaces, methods));

            if (ShouldAnalyzeCategory(IssueCategory.Design, options))
                issues.AddRange(AnalyzeDesign(filePath, root, classes, methods));

            if (ShouldAnalyzeCategory(IssueCategory.Performance, options))
                issues.AddRange(AnalyzePerformance(filePath, root, code));

            if (ShouldAnalyzeCategory(IssueCategory.Security, options))
                issues.AddRange(AnalyzeSecurity(filePath, root, code));

            if (ShouldAnalyzeCategory(IssueCategory.Maintainability, options))
                issues.AddRange(AnalyzeMaintainability(filePath, root, methods));

            // Filter issues by severity
            issues = issues
                .Where(i =>
                    (i.Severity == IssueSeverity.Error && options.IncludeErrors) ||
                    (i.Severity == IssueSeverity.Warning && options.IncludeWarnings) ||
                    (i.Severity == IssueSeverity.Info && options.IncludeInfo))
                .ToList();

            fileStats.IssueCount = issues.Count;
            result.FileStats.Add(fileStats);
            result.Issues.AddRange(issues);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to analyze file: {FilePath}", filePath);
        }
    }

    private static bool ShouldAnalyzeCategory(IssueCategory category, CSharpStaticAnalysisOptions options)
    {
        return options.Categories.Count == 0 || options.Categories.Contains(category);
    }

    private IEnumerable<StaticAnalysisIssue> AnalyzeNamingConventions(string filePath, SyntaxNode root, string code)
    {
        var issues = new List<StaticAnalysisIssue>();
        var lines = code.Split('\n');

        // Check class naming (should be PascalCase)
        foreach (var classDecl in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
        {
            var className = classDecl.Identifier.Text;
            if (!IsPascalCase(className))
            {
                var lineSpan = classDecl.GetLocation().GetLineSpan();
                issues.Add(new StaticAnalysisIssue
                {
                    FilePath = filePath,
                    Line = lineSpan.StartLinePosition.Line + 1,
                    Column = lineSpan.StartLinePosition.Character + 1,
                    Severity = IssueSeverity.Warning,
                    Category = IssueCategory.Naming,
                    RuleId = "SA1300",
                    Message = $"Class name '{className}' should use PascalCase",
                    CodeSnippet = GetLineContent(lines, lineSpan.StartLinePosition.Line),
                    SuggestedFix = $"Rename to '{ToPascalCase(className)}'"
                });
            }
        }

        // Check interface naming (should start with 'I')
        foreach (var interfaceDecl in root.DescendantNodes().OfType<InterfaceDeclarationSyntax>())
        {
            var interfaceName = interfaceDecl.Identifier.Text;
            if (!interfaceName.StartsWith("I") || interfaceName.Length < 2 || !char.IsUpper(interfaceName[1]))
            {
                var lineSpan = interfaceDecl.GetLocation().GetLineSpan();
                issues.Add(new StaticAnalysisIssue
                {
                    FilePath = filePath,
                    Line = lineSpan.StartLinePosition.Line + 1,
                    Column = lineSpan.StartLinePosition.Character + 1,
                    Severity = IssueSeverity.Warning,
                    Category = IssueCategory.Naming,
                    RuleId = "SA1302",
                    Message = $"Interface name '{interfaceName}' should start with 'I' followed by uppercase letter",
                    CodeSnippet = GetLineContent(lines, lineSpan.StartLinePosition.Line)
                });
            }
        }

        // Check method naming (should be PascalCase)
        foreach (var methodDecl in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
        {
            var methodName = methodDecl.Identifier.Text;
            if (!IsPascalCase(methodName))
            {
                var lineSpan = methodDecl.GetLocation().GetLineSpan();
                issues.Add(new StaticAnalysisIssue
                {
                    FilePath = filePath,
                    Line = lineSpan.StartLinePosition.Line + 1,
                    Column = lineSpan.StartLinePosition.Character + 1,
                    Severity = IssueSeverity.Warning,
                    Category = IssueCategory.Naming,
                    RuleId = "SA1300",
                    Message = $"Method name '{methodName}' should use PascalCase",
                    CodeSnippet = GetLineContent(lines, lineSpan.StartLinePosition.Line)
                });
            }
        }

        // Check private field naming (should start with underscore and be camelCase)
        foreach (var fieldDecl in root.DescendantNodes().OfType<FieldDeclarationSyntax>())
        {
            var isPrivate = fieldDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.PrivateKeyword)) ||
                           !fieldDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword) ||
                                                         m.IsKind(SyntaxKind.ProtectedKeyword) ||
                                                         m.IsKind(SyntaxKind.InternalKeyword));

            if (isPrivate && !fieldDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.ConstKeyword)))
            {
                foreach (var variable in fieldDecl.Declaration.Variables)
                {
                    var fieldName = variable.Identifier.Text;
                    if (!fieldName.StartsWith("_") || (fieldName.Length > 1 && char.IsUpper(fieldName[1])))
                    {
                        var lineSpan = fieldDecl.GetLocation().GetLineSpan();
                        issues.Add(new StaticAnalysisIssue
                        {
                            FilePath = filePath,
                            Line = lineSpan.StartLinePosition.Line + 1,
                            Column = lineSpan.StartLinePosition.Character + 1,
                            Severity = IssueSeverity.Info,
                            Category = IssueCategory.Naming,
                            RuleId = "SA1309",
                            Message = $"Private field '{fieldName}' should use _camelCase naming",
                            CodeSnippet = GetLineContent(lines, lineSpan.StartLinePosition.Line)
                        });
                    }
                }
            }
        }

        // Check constant naming (should be PascalCase or UPPER_CASE)
        foreach (var fieldDecl in root.DescendantNodes().OfType<FieldDeclarationSyntax>())
        {
            if (fieldDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.ConstKeyword)))
            {
                foreach (var variable in fieldDecl.Declaration.Variables)
                {
                    var constName = variable.Identifier.Text;
                    if (!IsPascalCase(constName) && !IsUpperSnakeCase(constName))
                    {
                        var lineSpan = fieldDecl.GetLocation().GetLineSpan();
                        issues.Add(new StaticAnalysisIssue
                        {
                            FilePath = filePath,
                            Line = lineSpan.StartLinePosition.Line + 1,
                            Column = lineSpan.StartLinePosition.Character + 1,
                            Severity = IssueSeverity.Info,
                            Category = IssueCategory.Naming,
                            RuleId = "SA1303",
                            Message = $"Constant '{constName}' should use PascalCase or UPPER_SNAKE_CASE",
                            CodeSnippet = GetLineContent(lines, lineSpan.StartLinePosition.Line)
                        });
                    }
                }
            }
        }

        return issues;
    }

    private IEnumerable<StaticAnalysisIssue> AnalyzeCodeStyle(string filePath, SyntaxNode root, string code)
    {
        var issues = new List<StaticAnalysisIssue>();
        // Normalize line endings to avoid false positives from \r
        var normalizedCode = code.Replace("\r\n", "\n").Replace("\r", "\n");
        var lines = normalizedCode.Split('\n');

        // Check for multiple statements on the same line
        var statements = root.DescendantNodes().OfType<StatementSyntax>().ToList();
        var statementsByLine = statements.GroupBy(s => s.GetLocation().GetLineSpan().StartLinePosition.Line);

        foreach (var lineGroup in statementsByLine)
        {
            if (lineGroup.Count() > 1)
            {
                var firstStatement = lineGroup.First();
                var lineSpan = firstStatement.GetLocation().GetLineSpan();
                issues.Add(new StaticAnalysisIssue
                {
                    FilePath = filePath,
                    Line = lineSpan.StartLinePosition.Line + 1,
                    Column = lineSpan.StartLinePosition.Character + 1,
                    Severity = IssueSeverity.Info,
                    Category = IssueCategory.Style,
                    RuleId = "SA1501",
                    Message = "Multiple statements on same line",
                    CodeSnippet = GetLineContent(lines, lineSpan.StartLinePosition.Line),
                    SuggestedFix = "Place each statement on its own line"
                });
            }
        }

        // Check for very long lines
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].Length > 200)
            {
                issues.Add(new StaticAnalysisIssue
                {
                    FilePath = filePath,
                    Line = i + 1,
                    Column = 1,
                    Severity = IssueSeverity.Info,
                    Category = IssueCategory.Style,
                    RuleId = "SA1505",
                    Message = $"Line is {lines[i].Length} characters long (recommended max: 200)",
                    CodeSnippet = lines[i].Substring(0, Math.Min(100, lines[i].Length)) + "...",
                    SuggestedFix = "Break line into multiple lines"
                });
            }
        }

        // Check for trailing whitespace
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].Length > 0 && lines[i].TrimEnd() != lines[i])
            {
                issues.Add(new StaticAnalysisIssue
                {
                    FilePath = filePath,
                    Line = i + 1,
                    Column = lines[i].TrimEnd().Length + 1,
                    Severity = IssueSeverity.Info,
                    Category = IssueCategory.Style,
                    RuleId = "SA1028",
                    Message = "Trailing whitespace detected",
                    SuggestedFix = "Remove trailing whitespace"
                });
            }
        }

        return issues;
    }

    private IEnumerable<StaticAnalysisIssue> AnalyzeCodeQuality(string filePath, SyntaxNode root, string code)
    {
        var issues = new List<StaticAnalysisIssue>();
        var lines = code.Split('\n');

        // Check for empty catch blocks
        foreach (var catchClause in root.DescendantNodes().OfType<CatchClauseSyntax>())
        {
            if (catchClause.Block.Statements.Count == 0)
            {
                var lineSpan = catchClause.GetLocation().GetLineSpan();
                issues.Add(new StaticAnalysisIssue
                {
                    FilePath = filePath,
                    Line = lineSpan.StartLinePosition.Line + 1,
                    Column = lineSpan.StartLinePosition.Character + 1,
                    Severity = IssueSeverity.Warning,
                    Category = IssueCategory.CodeQuality,
                    RuleId = "CA1031",
                    Message = "Empty catch block swallows exceptions silently",
                    CodeSnippet = GetLineContent(lines, lineSpan.StartLinePosition.Line),
                    SuggestedFix = "Add logging or rethrow the exception"
                });
            }
        }

        // Check for catch-all without logging
        foreach (var catchClause in root.DescendantNodes().OfType<CatchClauseSyntax>())
        {
            if (catchClause.Declaration == null ||
                catchClause.Declaration.Type.ToString() == "Exception" ||
                catchClause.Declaration.Type.ToString() == "System.Exception")
            {
                var blockContent = catchClause.Block.ToString().ToLower();
                if (!blockContent.Contains("log") && !blockContent.Contains("throw"))
                {
                    var lineSpan = catchClause.GetLocation().GetLineSpan();
                    issues.Add(new StaticAnalysisIssue
                    {
                        FilePath = filePath,
                        Line = lineSpan.StartLinePosition.Line + 1,
                        Column = lineSpan.StartLinePosition.Character + 1,
                        Severity = IssueSeverity.Warning,
                        Category = IssueCategory.CodeQuality,
                        RuleId = "CA1031",
                        Message = "Catching general Exception without logging",
                        CodeSnippet = GetLineContent(lines, lineSpan.StartLinePosition.Line),
                        SuggestedFix = "Catch specific exceptions or add logging"
                    });
                }
            }
        }

        // Check for magic numbers
        foreach (var literal in root.DescendantNodes().OfType<LiteralExpressionSyntax>())
        {
            if (literal.IsKind(SyntaxKind.NumericLiteralExpression))
            {
                var value = literal.Token.ValueText;
                if (int.TryParse(value, out int num) && num != 0 && num != 1 && num != -1 && num != 2)
                {
                    // Check if it's inside a constant or field initialization
                    var parent = literal.Parent;
                    var isInConstant = false;
                    while (parent != null)
                    {
                        if (parent is FieldDeclarationSyntax field &&
                            field.Modifiers.Any(m => m.IsKind(SyntaxKind.ConstKeyword)))
                        {
                            isInConstant = true;
                            break;
                        }
                        if (parent is LocalDeclarationStatementSyntax local &&
                            local.Modifiers.Any(m => m.IsKind(SyntaxKind.ConstKeyword)))
                        {
                            isInConstant = true;
                            break;
                        }
                        parent = parent.Parent;
                    }

                    if (!isInConstant)
                    {
                        var lineSpan = literal.GetLocation().GetLineSpan();
                        issues.Add(new StaticAnalysisIssue
                        {
                            FilePath = filePath,
                            Line = lineSpan.StartLinePosition.Line + 1,
                            Column = lineSpan.StartLinePosition.Character + 1,
                            Severity = IssueSeverity.Info,
                            Category = IssueCategory.CodeQuality,
                            RuleId = "CA1500",
                            Message = $"Magic number '{value}' - consider using a named constant",
                            CodeSnippet = GetLineContent(lines, lineSpan.StartLinePosition.Line),
                            SuggestedFix = "Extract to a named constant"
                        });
                    }
                }
            }
        }

        // Check for TODO comments
        for (int i = 0; i < lines.Length; i++)
        {
            if (Regex.IsMatch(lines[i], @"//\s*(TODO|FIXME|HACK|BUG)", RegexOptions.IgnoreCase))
            {
                issues.Add(new StaticAnalysisIssue
                {
                    FilePath = filePath,
                    Line = i + 1,
                    Column = 1,
                    Severity = IssueSeverity.Info,
                    Category = IssueCategory.CodeQuality,
                    RuleId = "TODO",
                    Message = "TODO/FIXME comment found",
                    CodeSnippet = lines[i].Trim()
                });
            }
        }

        return issues;
    }

    private IEnumerable<StaticAnalysisIssue> AnalyzeUnusedCode(string filePath, SyntaxNode root, string code)
    {
        var issues = new List<StaticAnalysisIssue>();
        var lines = code.Split('\n');

        // Check for using directives that might be unnecessary (basic heuristic)
        // Using directives can be in the compilation unit or within namespace declarations
        var usingDirectives = new List<UsingDirectiveSyntax>();
        
        // Check at compilation unit level
        if (root is CompilationUnitSyntax compilationUnit)
        {
            usingDirectives.AddRange(compilationUnit.Usings);
        }
        
        // Check within namespaces
        foreach (var namespaceDecl in root.DescendantNodes().OfType<BaseNamespaceDeclarationSyntax>())
        {
            usingDirectives.AddRange(namespaceDecl.Usings);
        }

        if (usingDirectives.Count == 0)
        {
            return issues;
        }

        // Get all identifiers used in the file (excluding the using directives themselves)
        var usedIdentifiers = new HashSet<string>();
        var contentNodes = root.DescendantNodes()
            .Where(n => !(n.Parent is UsingDirectiveSyntax) && !(n is UsingDirectiveSyntax));

        foreach (var identifier in contentNodes.OfType<IdentifierNameSyntax>())
        {
            usedIdentifiers.Add(identifier.Identifier.Text);
        }
        foreach (var qualified in contentNodes.OfType<QualifiedNameSyntax>())
        {
            usedIdentifiers.Add(qualified.ToString());
            // Also add individual parts
            foreach (var part in qualified.ToString().Split('.'))
            {
                usedIdentifiers.Add(part);
            }
        }

        // Common System namespaces that are often used implicitly with implicit usings
        var implicitlyUsedNamespaces = new HashSet<string>
        {
            "System", "System.Collections.Generic", "System.Linq", "System.Threading.Tasks",
            "System.IO", "System.Text"
        };

        foreach (var usingDirective in usingDirectives)
        {
            var namespaceName = usingDirective.Name?.ToString() ?? "";
            if (string.IsNullOrEmpty(namespaceName))
                continue;

            // Skip implicitly used namespaces
            if (implicitlyUsedNamespaces.Contains(namespaceName))
                continue;

            var namespaceParts = namespaceName.Split('.');
            var lastPart = namespaceParts.Last();

            // Check if any type from this namespace might be used
            // A using is potentially unused if:
            // 1. No identifier matches the last part of the namespace
            // 2. No identifier matches any part of the namespace
            var isUsed = usedIdentifiers.Contains(lastPart) ||
                        namespaceParts.Any(part => usedIdentifiers.Contains(part));

            if (!isUsed)
            {
                var lineSpan = usingDirective.GetLocation().GetLineSpan();
                issues.Add(new StaticAnalysisIssue
                {
                    FilePath = filePath,
                    Line = lineSpan.StartLinePosition.Line + 1,
                    Column = lineSpan.StartLinePosition.Character + 1,
                    Severity = IssueSeverity.Info,
                    Category = IssueCategory.UnusedCode,
                    RuleId = "IDE0005",
                    Message = $"Potentially unused using directive: {namespaceName}",
                    CodeSnippet = GetLineContent(lines, lineSpan.StartLinePosition.Line),
                    SuggestedFix = "Remove if not needed"
                });
            }
        }

        return issues;
    }

    private IEnumerable<StaticAnalysisIssue> AnalyzeDocumentation(
        string filePath,
        SyntaxNode root,
        List<ClassDeclarationSyntax> classes,
        List<InterfaceDeclarationSyntax> interfaces,
        List<MethodDeclarationSyntax> methods)
    {
        var issues = new List<StaticAnalysisIssue>();
        var code = root.ToFullString();
        var lines = code.Split('\n');

        // Check public classes for XML documentation
        foreach (var classDecl in classes)
        {
            if (classDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword)))
            {
                var hasDocComment = classDecl.GetLeadingTrivia()
                    .Any(t => t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
                             t.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia));

                if (!hasDocComment)
                {
                    var lineSpan = classDecl.GetLocation().GetLineSpan();
                    issues.Add(new StaticAnalysisIssue
                    {
                        FilePath = filePath,
                        Line = lineSpan.StartLinePosition.Line + 1,
                        Column = lineSpan.StartLinePosition.Character + 1,
                        Severity = IssueSeverity.Info,
                        Category = IssueCategory.Documentation,
                        RuleId = "SA1600",
                        Message = $"Public class '{classDecl.Identifier.Text}' is missing XML documentation",
                        CodeSnippet = GetLineContent(lines, lineSpan.StartLinePosition.Line),
                        SuggestedFix = "Add /// <summary> documentation"
                    });
                }
            }
        }

        // Check public interfaces for XML documentation
        foreach (var interfaceDecl in interfaces)
        {
            if (interfaceDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword)))
            {
                var hasDocComment = interfaceDecl.GetLeadingTrivia()
                    .Any(t => t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
                             t.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia));

                if (!hasDocComment)
                {
                    var lineSpan = interfaceDecl.GetLocation().GetLineSpan();
                    issues.Add(new StaticAnalysisIssue
                    {
                        FilePath = filePath,
                        Line = lineSpan.StartLinePosition.Line + 1,
                        Column = lineSpan.StartLinePosition.Character + 1,
                        Severity = IssueSeverity.Info,
                        Category = IssueCategory.Documentation,
                        RuleId = "SA1600",
                        Message = $"Public interface '{interfaceDecl.Identifier.Text}' is missing XML documentation",
                        CodeSnippet = GetLineContent(lines, lineSpan.StartLinePosition.Line),
                        SuggestedFix = "Add /// <summary> documentation"
                    });
                }
            }
        }

        // Check public methods for XML documentation
        foreach (var methodDecl in methods)
        {
            if (methodDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword)))
            {
                var hasDocComment = methodDecl.GetLeadingTrivia()
                    .Any(t => t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
                             t.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia));

                if (!hasDocComment)
                {
                    var lineSpan = methodDecl.GetLocation().GetLineSpan();
                    issues.Add(new StaticAnalysisIssue
                    {
                        FilePath = filePath,
                        Line = lineSpan.StartLinePosition.Line + 1,
                        Column = lineSpan.StartLinePosition.Character + 1,
                        Severity = IssueSeverity.Info,
                        Category = IssueCategory.Documentation,
                        RuleId = "SA1600",
                        Message = $"Public method '{methodDecl.Identifier.Text}' is missing XML documentation",
                        CodeSnippet = GetLineContent(lines, lineSpan.StartLinePosition.Line),
                        SuggestedFix = "Add /// <summary> documentation"
                    });
                }
            }
        }

        return issues;
    }

    private IEnumerable<StaticAnalysisIssue> AnalyzeDesign(
        string filePath,
        SyntaxNode root,
        List<ClassDeclarationSyntax> classes,
        List<MethodDeclarationSyntax> methods)
    {
        var issues = new List<StaticAnalysisIssue>();
        var code = root.ToFullString();
        var lines = code.Split('\n');

        // Check for classes with too many methods
        foreach (var classDecl in classes)
        {
            var classMethods = classDecl.Members.OfType<MethodDeclarationSyntax>().Count();
            if (classMethods > 20)
            {
                var lineSpan = classDecl.GetLocation().GetLineSpan();
                issues.Add(new StaticAnalysisIssue
                {
                    FilePath = filePath,
                    Line = lineSpan.StartLinePosition.Line + 1,
                    Column = lineSpan.StartLinePosition.Character + 1,
                    Severity = IssueSeverity.Warning,
                    Category = IssueCategory.Design,
                    RuleId = "CA1502",
                    Message = $"Class '{classDecl.Identifier.Text}' has {classMethods} methods - consider splitting",
                    CodeSnippet = GetLineContent(lines, lineSpan.StartLinePosition.Line),
                    SuggestedFix = "Split into smaller, focused classes"
                });
            }
        }

        // Check for methods with too many parameters
        foreach (var methodDecl in methods)
        {
            var paramCount = methodDecl.ParameterList.Parameters.Count;
            if (paramCount > 7)
            {
                var lineSpan = methodDecl.GetLocation().GetLineSpan();
                issues.Add(new StaticAnalysisIssue
                {
                    FilePath = filePath,
                    Line = lineSpan.StartLinePosition.Line + 1,
                    Column = lineSpan.StartLinePosition.Character + 1,
                    Severity = IssueSeverity.Warning,
                    Category = IssueCategory.Design,
                    RuleId = "CA1026",
                    Message = $"Method '{methodDecl.Identifier.Text}' has {paramCount} parameters - consider using a parameter object",
                    CodeSnippet = GetLineContent(lines, lineSpan.StartLinePosition.Line),
                    SuggestedFix = "Use a parameter object or builder pattern"
                });
            }
        }

        // Check for deeply nested code
        foreach (var methodDecl in methods)
        {
            var maxDepth = GetMaxNestingDepth(methodDecl);
            if (maxDepth > 4)
            {
                var lineSpan = methodDecl.GetLocation().GetLineSpan();
                issues.Add(new StaticAnalysisIssue
                {
                    FilePath = filePath,
                    Line = lineSpan.StartLinePosition.Line + 1,
                    Column = lineSpan.StartLinePosition.Character + 1,
                    Severity = IssueSeverity.Warning,
                    Category = IssueCategory.Design,
                    RuleId = "CA1502",
                    Message = $"Method '{methodDecl.Identifier.Text}' has nesting depth of {maxDepth} - consider refactoring",
                    CodeSnippet = GetLineContent(lines, lineSpan.StartLinePosition.Line),
                    SuggestedFix = "Extract nested logic into separate methods"
                });
            }
        }

        return issues;
    }

    private IEnumerable<StaticAnalysisIssue> AnalyzePerformance(string filePath, SyntaxNode root, string code)
    {
        var issues = new List<StaticAnalysisIssue>();
        var lines = code.Split('\n');

        // Get all loop statements
        var loops = root.DescendantNodes()
            .Where(n => n is ForStatementSyntax || n is ForEachStatementSyntax ||
                       n is WhileStatementSyntax || n is DoStatementSyntax)
            .ToList();

        // Check for string concatenation in loops
        foreach (var loop in loops)
        {
            var assignments = loop.DescendantNodes().OfType<AssignmentExpressionSyntax>();
            foreach (var assignment in assignments)
            {
                // Check for += operator with string operand
                if (assignment.IsKind(SyntaxKind.AddAssignmentExpression))
                {
                    // Check if the right side involves ToString() or is a string literal/interpolation
                    var rightStr = assignment.Right.ToString();
                    var leftStr = assignment.Left.ToString();

                    // Simple heuristic: if += involves .ToString() or string operations
                    if (rightStr.Contains(".ToString()") ||
                        assignment.Right is InterpolatedStringExpressionSyntax ||
                        assignment.Right is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.StringLiteralExpression))
                    {
                        var lineSpan = assignment.GetLocation().GetLineSpan();
                        issues.Add(new StaticAnalysisIssue
                        {
                            FilePath = filePath,
                            Line = lineSpan.StartLinePosition.Line + 1,
                            Column = lineSpan.StartLinePosition.Character + 1,
                            Severity = IssueSeverity.Warning,
                            Category = IssueCategory.Performance,
                            RuleId = "CA1850",
                            Message = "String concatenation in loop - consider using StringBuilder",
                            CodeSnippet = GetLineContent(lines, lineSpan.StartLinePosition.Line),
                            SuggestedFix = "Use StringBuilder for better performance"
                        });
                    }
                }
            }
        }

        // Check for LINQ in loops
        foreach (var loop in loops)
        {
            var invocations = loop.DescendantNodes().OfType<InvocationExpressionSyntax>();
            foreach (var invocation in invocations)
            {
                // Get the method being called
                string methodName = string.Empty;
                if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
                {
                    methodName = memberAccess.Name.Identifier.Text;
                }
                else if (invocation.Expression is IdentifierNameSyntax identifierName)
                {
                    methodName = identifierName.Identifier.Text;
                }

                // Check if it's a common LINQ method
                var linqMethods = new[] { "Count", "Any", "First", "ToList", "FirstOrDefault", "Last", "ToArray", "Where", "Select", "OrderBy", "OrderByDescending" };
                if (linqMethods.Contains(methodName))
                {
                    var lineSpan = invocation.GetLocation().GetLineSpan();
                    issues.Add(new StaticAnalysisIssue
                    {
                        FilePath = filePath,
                        Line = lineSpan.StartLinePosition.Line + 1,
                        Column = lineSpan.StartLinePosition.Character + 1,
                        Severity = IssueSeverity.Warning,
                        Category = IssueCategory.Performance,
                        RuleId = "CA1851",
                        Message = "LINQ operation inside loop may cause repeated enumeration",
                        CodeSnippet = GetLineContent(lines, lineSpan.StartLinePosition.Line),
                        SuggestedFix = "Evaluate LINQ result before the loop"
                    });
                }
            }
        }

        return issues;
    }

    private IEnumerable<StaticAnalysisIssue> AnalyzeSecurity(string filePath, SyntaxNode root, string code)
    {
        var issues = new List<StaticAnalysisIssue>();
        var lines = code.Split('\n');

        // Check for hardcoded passwords or secrets
        var sensitivePatterns = new[]
        {
            ("password", @"password\s*=\s*""[^""]+"""),
            ("secret", @"secret\s*=\s*""[^""]+"""),
            ("api key", @"apikey\s*=\s*""[^""]+"""),
            ("connection string", @"connectionstring\s*=\s*""[^""]+""")
        };

        foreach (var (name, pattern) in sensitivePatterns)
        {
            var matches = Regex.Matches(code, pattern, RegexOptions.IgnoreCase);
            foreach (Match match in matches)
            {
                var lineNumber = code.Substring(0, match.Index).Count(c => c == '\n');
                issues.Add(new StaticAnalysisIssue
                {
                    FilePath = filePath,
                    Line = lineNumber + 1,
                    Column = 1,
                    Severity = IssueSeverity.Error,
                    Category = IssueCategory.Security,
                    RuleId = "CA2100",
                    Message = $"Potential hardcoded {name} detected",
                    CodeSnippet = GetLineContent(lines, lineNumber),
                    SuggestedFix = "Use configuration or secret management"
                });
            }
        }

        // Track variables that are built with string concatenation
        var concatenatedVariables = new HashSet<string>();
        foreach (var localDecl in root.DescendantNodes().OfType<LocalDeclarationStatementSyntax>())
        {
            foreach (var variable in localDecl.Declaration.Variables)
            {
                if (variable.Initializer != null)
                {
                    var initValue = variable.Initializer.Value.ToString();
                    // Check if initialized with concatenation or interpolation
                    if (initValue.Contains("+") && initValue.Contains("\"") ||
                        initValue.StartsWith("$\""))
                    {
                        concatenatedVariables.Add(variable.Identifier.Text);
                    }
                }
            }
        }

        // Also check assignments
        foreach (var assignment in root.DescendantNodes().OfType<AssignmentExpressionSyntax>())
        {
            if (assignment.Left is IdentifierNameSyntax identifier)
            {
                var rightValue = assignment.Right.ToString();
                if (rightValue.Contains("+") && rightValue.Contains("\"") ||
                    rightValue.StartsWith("$\""))
                {
                    concatenatedVariables.Add(identifier.Identifier.Text);
                }
            }
        }

        // Check for SQL injection vulnerabilities
        foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            var methodName = invocation.Expression.ToString();
            if (methodName.Contains("ExecuteSqlRaw") || methodName.Contains("FromSqlRaw"))
            {
                var args = invocation.ArgumentList.Arguments;
                if (args.Count > 0)
                {
                    var firstArg = args[0].ToString();
                    var isSqlInjection = false;

                    // Direct concatenation or interpolation in argument
                    if (firstArg.Contains("+") || firstArg.StartsWith("$\""))
                    {
                        isSqlInjection = true;
                    }
                    // Variable that was built with concatenation
                    else if (concatenatedVariables.Contains(firstArg))
                    {
                        isSqlInjection = true;
                    }

                    if (isSqlInjection)
                    {
                        var lineSpan = invocation.GetLocation().GetLineSpan();
                        issues.Add(new StaticAnalysisIssue
                        {
                            FilePath = filePath,
                            Line = lineSpan.StartLinePosition.Line + 1,
                            Column = lineSpan.StartLinePosition.Character + 1,
                            Severity = IssueSeverity.Error,
                            Category = IssueCategory.Security,
                            RuleId = "CA3001",
                            Message = "Potential SQL injection - avoid string concatenation in SQL queries",
                            CodeSnippet = GetLineContent(lines, lineSpan.StartLinePosition.Line),
                            SuggestedFix = "Use parameterized queries"
                        });
                    }
                }
            }
        }

        return issues;
    }

    private IEnumerable<StaticAnalysisIssue> AnalyzeMaintainability(
        string filePath,
        SyntaxNode root,
        List<MethodDeclarationSyntax> methods)
    {
        var issues = new List<StaticAnalysisIssue>();
        var code = root.ToFullString();
        var lines = code.Split('\n');

        // Check for long methods
        foreach (var methodDecl in methods)
        {
            var methodLines = methodDecl.GetLocation().GetLineSpan();
            var lineCount = methodLines.EndLinePosition.Line - methodLines.StartLinePosition.Line;

            if (lineCount > 50)
            {
                issues.Add(new StaticAnalysisIssue
                {
                    FilePath = filePath,
                    Line = methodLines.StartLinePosition.Line + 1,
                    Column = methodLines.StartLinePosition.Character + 1,
                    Severity = IssueSeverity.Warning,
                    Category = IssueCategory.Maintainability,
                    RuleId = "CA1502",
                    Message = $"Method '{methodDecl.Identifier.Text}' is {lineCount} lines long - consider breaking it up",
                    CodeSnippet = GetLineContent(lines, methodLines.StartLinePosition.Line),
                    SuggestedFix = "Extract logic into smaller, focused methods"
                });
            }
        }

        // Check for high cyclomatic complexity (basic heuristic)
        foreach (var methodDecl in methods)
        {
            var complexity = CalculateCyclomaticComplexity(methodDecl);
            if (complexity > 15)
            {
                var lineSpan = methodDecl.GetLocation().GetLineSpan();
                issues.Add(new StaticAnalysisIssue
                {
                    FilePath = filePath,
                    Line = lineSpan.StartLinePosition.Line + 1,
                    Column = lineSpan.StartLinePosition.Character + 1,
                    Severity = IssueSeverity.Warning,
                    Category = IssueCategory.Maintainability,
                    RuleId = "CA1502",
                    Message = $"Method '{methodDecl.Identifier.Text}' has high cyclomatic complexity ({complexity})",
                    CodeSnippet = GetLineContent(lines, lineSpan.StartLinePosition.Line),
                    SuggestedFix = "Simplify logic or extract into multiple methods"
                });
            }
        }

        return issues;
    }

    private static int GetMaxNestingDepth(SyntaxNode node)
    {
        int maxDepth = 0;

        void Visit(SyntaxNode n, int depth)
        {
            if (n is IfStatementSyntax || n is ForStatementSyntax ||
                n is ForEachStatementSyntax || n is WhileStatementSyntax ||
                n is DoStatementSyntax || n is SwitchStatementSyntax ||
                n is TryStatementSyntax)
            {
                depth++;
                maxDepth = Math.Max(maxDepth, depth);
            }

            foreach (var child in n.ChildNodes())
            {
                Visit(child, depth);
            }
        }

        Visit(node, 0);
        return maxDepth;
    }

    private static int CalculateCyclomaticComplexity(MethodDeclarationSyntax method)
    {
        int complexity = 1; // Base complexity

        foreach (var node in method.DescendantNodes())
        {
            if (node is IfStatementSyntax ||
                node is ConditionalExpressionSyntax ||
                node is CaseSwitchLabelSyntax ||
                node is ForStatementSyntax ||
                node is ForEachStatementSyntax ||
                node is WhileStatementSyntax ||
                node is DoStatementSyntax ||
                node is CatchClauseSyntax)
            {
                complexity++;
            }

            if (node is BinaryExpressionSyntax binary)
            {
                if (binary.IsKind(SyntaxKind.LogicalAndExpression) ||
                    binary.IsKind(SyntaxKind.LogicalOrExpression) ||
                    binary.IsKind(SyntaxKind.CoalesceExpression))
                {
                    complexity++;
                }
            }
        }

        return complexity;
    }

    private void CalculateSummary(CSharpStaticAnalysisResult result, long durationMs)
    {
        result.Summary.TotalFiles = result.FileStats.Count;
        result.Summary.TotalLinesOfCode = result.FileStats.Sum(f => f.LinesOfCode);
        result.Summary.TotalClasses = result.FileStats.Sum(f => f.ClassCount);
        result.Summary.TotalInterfaces = result.FileStats.Sum(f => f.InterfaceCount);
        result.Summary.TotalMethods = result.FileStats.Sum(f => f.MethodCount);
        result.Summary.TotalIssues = result.Issues.Count;

        result.Summary.ErrorCount = result.Issues.Count(i => i.Severity == IssueSeverity.Error);
        result.Summary.WarningCount = result.Issues.Count(i => i.Severity == IssueSeverity.Warning);
        result.Summary.InfoCount = result.Issues.Count(i => i.Severity == IssueSeverity.Info);

        result.Summary.IssuesByCategory = result.Issues
            .GroupBy(i => i.Category)
            .ToDictionary(g => g.Key, g => g.Count());

        result.Summary.AnalysisDurationMs = durationMs;
    }

    private static string? GetLineContent(string[] lines, int lineIndex)
    {
        if (lineIndex >= 0 && lineIndex < lines.Length)
            return lines[lineIndex];
        return null;
    }

    private static bool IsPascalCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        return char.IsUpper(name[0]) && !name.Contains("_");
    }

    private static bool IsUpperSnakeCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        return name.All(c => char.IsUpper(c) || char.IsDigit(c) || c == '_');
    }

    private static string ToPascalCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        return char.ToUpper(name[0]) + name.Substring(1);
    }
}
