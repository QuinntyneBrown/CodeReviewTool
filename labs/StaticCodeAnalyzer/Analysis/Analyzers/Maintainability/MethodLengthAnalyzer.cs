using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StaticCodeAnalyzer.Analysis.Analyzers.Maintainability;

public class MethodLengthAnalyzer : BaseCodeAnalyzer
{
    public override string AnalyzerId => "MAINT002";
    public override string Name => "Method Length Analyzer";
    public override IssueCategory Category => IssueCategory.Maintainability;

    private const int WarningThreshold = 30;
    private const int CriticalThreshold = 60;
    private const int BlockerThreshold = 100;

    public override Task<IEnumerable<AnalysisResult>> AnalyzeAsync(
        SyntaxTree syntaxTree,
        SemanticModel? semanticModel,
        string filePath)
    {
        var results = new List<AnalysisResult>();
        var root = syntaxTree.GetRoot();

        // Check methods
        var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();
        foreach (var method in methods)
        {
            var lineCount = CountExecutableLines(method);
            var methodName = method.Identifier.Text;

            if (lineCount >= BlockerThreshold)
            {
                results.Add(CreateResult(
                    "MAINT002",
                    "Extremely Long Method",
                    $"Method '{methodName}' has {lineCount} lines of code. Maximum recommended is {WarningThreshold}.",
                    filePath,
                    method.Identifier.GetLocation(),
                    Severity.Blocker,
                    $"{methodName}() - {lineCount} lines",
                    "Split this method into smaller, focused methods. Each method should do one thing well."));
            }
            else if (lineCount >= CriticalThreshold)
            {
                results.Add(CreateResult(
                    "MAINT002",
                    "Very Long Method",
                    $"Method '{methodName}' has {lineCount} lines of code. Maximum recommended is {WarningThreshold}.",
                    filePath,
                    method.Identifier.GetLocation(),
                    Severity.Critical,
                    $"{methodName}() - {lineCount} lines",
                    "This method is too long. Consider breaking it into smaller methods."));
            }
            else if (lineCount >= WarningThreshold)
            {
                results.Add(CreateResult(
                    "MAINT002",
                    "Long Method",
                    $"Method '{methodName}' has {lineCount} lines of code. Maximum recommended is {WarningThreshold}.",
                    filePath,
                    method.Identifier.GetLocation(),
                    Severity.Major,
                    $"{methodName}() - {lineCount} lines",
                    "Consider refactoring this method to improve readability."));
            }
        }

        // Check constructors
        var constructors = root.DescendantNodes().OfType<ConstructorDeclarationSyntax>();
        foreach (var ctor in constructors)
        {
            var lineCount = CountExecutableLines(ctor);
            var className = ctor.Identifier.Text;

            if (lineCount >= CriticalThreshold)
            {
                results.Add(CreateResult(
                    "MAINT002",
                    "Very Long Constructor",
                    $"Constructor for '{className}' has {lineCount} lines. Constructors should be simple.",
                    filePath,
                    ctor.Identifier.GetLocation(),
                    Severity.Major,
                    $"{className}() constructor - {lineCount} lines",
                    "Move initialization logic to separate methods or use factory patterns."));
            }
        }

        // Check property accessors
        var properties = root.DescendantNodes().OfType<PropertyDeclarationSyntax>();
        foreach (var property in properties)
        {
            if (property.AccessorList != null)
            {
                foreach (var accessor in property.AccessorList.Accessors)
                {
                    if (accessor.Body != null)
                    {
                        var lineCount = CountLinesInBlock(accessor.Body);
                        if (lineCount >= 20)
                        {
                            var accessorType = accessor.Keyword.Text;
                            results.Add(CreateResult(
                                "MAINT002",
                                "Long Property Accessor",
                                $"Property '{property.Identifier.Text}' {accessorType}ter has {lineCount} lines.",
                                filePath,
                                accessor.GetLocation(),
                                Severity.Major,
                                $"{property.Identifier.Text}.{accessorType} - {lineCount} lines",
                                "Extract complex property logic into a method."));
                        }
                    }
                }
            }
        }

        return Task.FromResult<IEnumerable<AnalysisResult>>(results);
    }

    private static int CountExecutableLines(BaseMethodDeclarationSyntax method)
    {
        if (method.Body == null && method.ExpressionBody == null)
            return 0;

        var methodSpan = method.GetLocation().GetLineSpan();
        int totalLines = methodSpan.EndLinePosition.Line - methodSpan.StartLinePosition.Line + 1;

        // Subtract blank lines and comment-only lines
        var text = method.ToString();
        var lines = text.Split('\n');

        int executableLines = 0;
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (!string.IsNullOrWhiteSpace(trimmed) &&
                !trimmed.StartsWith("//") &&
                !trimmed.StartsWith("/*") &&
                !trimmed.StartsWith("*") &&
                trimmed != "{" &&
                trimmed != "}")
            {
                executableLines++;
            }
        }

        return executableLines;
    }

    private static int CountLinesInBlock(BlockSyntax block)
    {
        var span = block.GetLocation().GetLineSpan();
        return span.EndLinePosition.Line - span.StartLinePosition.Line + 1;
    }
}
