using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StaticCodeAnalyzer.Analysis.Analyzers.Security;

public class PathTraversalAnalyzer : BaseCodeAnalyzer
{
    public override string AnalyzerId => "SEC004";
    public override string Name => "Path Traversal Analyzer";
    public override IssueCategory Category => IssueCategory.Security;

    private static readonly HashSet<string> FileOperationMethods = new()
    {
        "ReadAllText", "ReadAllBytes", "ReadAllLines", "ReadLines",
        "WriteAllText", "WriteAllBytes", "WriteAllLines",
        "Open", "OpenRead", "OpenWrite", "OpenText",
        "Create", "CreateText", "Delete", "Copy", "Move",
        "Exists", "GetAttributes", "SetAttributes",
        "ReadAllTextAsync", "ReadAllBytesAsync", "WriteAllTextAsync", "WriteAllBytesAsync"
    };

    private static readonly HashSet<string> PathMethods = new()
    {
        "Combine", "Join", "GetFullPath"
    };

    public override Task<IEnumerable<AnalysisResult>> AnalyzeAsync(
        SyntaxTree syntaxTree,
        SemanticModel? semanticModel,
        string filePath)
    {
        var results = new List<AnalysisResult>();
        var root = syntaxTree.GetRoot();

        var invocations = root.DescendantNodes().OfType<InvocationExpressionSyntax>();

        foreach (var invocation in invocations)
        {
            var methodName = GetMethodName(invocation);
            var fullName = invocation.Expression.ToString();

            // Check for file operations with dynamic paths
            if (FileOperationMethods.Contains(methodName) ||
                (fullName.Contains("File.") || fullName.Contains("Directory.")))
            {
                var args = invocation.ArgumentList.Arguments;
                if (args.Any() && IsUnsanitizedPath(args.First().Expression))
                {
                    results.Add(CreateResult(
                        "SEC004",
                        "Potential Path Traversal Vulnerability",
                        $"File operation '{methodName}' uses potentially unsanitized path input.",
                        filePath,
                        invocation.GetLocation(),
                        Severity.Critical,
                        GetCodeSnippet(invocation),
                        "Validate and sanitize file paths. Use Path.GetFullPath and verify the path is within allowed directories.",
                        "CWE-22",
                        "A01:2021 - Broken Access Control"));
                }
            }

            // Check for Path.Combine with user input
            if (PathMethods.Contains(methodName) && fullName.Contains("Path."))
            {
                var args = invocation.ArgumentList.Arguments;
                if (args.Any(a => IsUserInputPath(a.Expression)))
                {
                    results.Add(CreateResult(
                        "SEC004",
                        "Potential Path Traversal via Path.Combine",
                        "Path.Combine or similar method used with user input. User input may contain '../' sequences.",
                        filePath,
                        invocation.GetLocation(),
                        Severity.Major,
                        GetCodeSnippet(invocation),
                        "After combining paths, validate the result is within the expected base directory.",
                        "CWE-22",
                        "A01:2021 - Broken Access Control"));
                }
            }

            // Check for StreamReader/StreamWriter with dynamic paths
            if (fullName.Contains("StreamReader") || fullName.Contains("StreamWriter") ||
                fullName.Contains("FileStream"))
            {
                var parent = invocation.Parent;
                if (parent is ObjectCreationExpressionSyntax creation)
                {
                    var args = creation.ArgumentList?.Arguments;
                    if (args != null && args.Value.Any(a => IsUnsanitizedPath(a.Expression)))
                    {
                        results.Add(CreateResult(
                            "SEC004",
                            "Potential Path Traversal in Stream Creation",
                            "Stream created with potentially unsanitized path.",
                            filePath,
                            creation.GetLocation(),
                            Severity.Critical,
                            GetCodeSnippet(creation),
                            "Validate file paths before creating streams.",
                            "CWE-22",
                            "A01:2021 - Broken Access Control"));
                    }
                }
            }
        }

        // Check object creations for FileInfo, DirectoryInfo
        var objectCreations = root.DescendantNodes().OfType<ObjectCreationExpressionSyntax>();
        foreach (var creation in objectCreations)
        {
            var typeName = creation.Type.ToString();
            if (typeName.Contains("FileInfo") || typeName.Contains("DirectoryInfo") ||
                typeName.Contains("StreamReader") || typeName.Contains("StreamWriter") ||
                typeName.Contains("FileStream"))
            {
                var args = creation.ArgumentList?.Arguments;
                if (args != null && args.Value.Any(a => IsUnsanitizedPath(a.Expression)))
                {
                    results.Add(CreateResult(
                        "SEC004",
                        "Potential Path Traversal in Object Creation",
                        $"{typeName} created with potentially unsanitized path.",
                        filePath,
                        creation.GetLocation(),
                        Severity.Critical,
                        GetCodeSnippet(creation),
                        "Validate and canonicalize file paths before use.",
                        "CWE-22",
                        "A01:2021 - Broken Access Control"));
                }
            }
        }

        return Task.FromResult<IEnumerable<AnalysisResult>>(results);
    }

    private static string GetMethodName(InvocationExpressionSyntax invocation)
    {
        return invocation.Expression switch
        {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.Text,
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            _ => string.Empty
        };
    }

    private static bool IsUnsanitizedPath(ExpressionSyntax expression)
    {
        return expression is IdentifierNameSyntax ||
               expression is BinaryExpressionSyntax ||
               expression is InterpolatedStringExpressionSyntax ||
               IsUserInputPath(expression);
    }

    private static bool IsUserInputPath(ExpressionSyntax expression)
    {
        var text = expression.ToString().ToLowerInvariant();
        var inputIndicators = new[] { "request", "input", "param", "query", "filename", "filepath", "path", "user", "upload" };
        return inputIndicators.Any(ind => text.Contains(ind));
    }
}
