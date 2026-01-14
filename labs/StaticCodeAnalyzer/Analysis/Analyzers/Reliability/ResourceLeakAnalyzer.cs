using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StaticCodeAnalyzer.Analysis.Analyzers.Reliability;

public class ResourceLeakAnalyzer : BaseCodeAnalyzer
{
    public override string AnalyzerId => "REL002";
    public override string Name => "Resource Leak Analyzer";
    public override IssueCategory Category => IssueCategory.Reliability;

    private static readonly HashSet<string> DisposableTypes = new()
    {
        "Stream", "FileStream", "MemoryStream", "NetworkStream", "BufferedStream",
        "StreamReader", "StreamWriter", "BinaryReader", "BinaryWriter",
        "TextReader", "TextWriter", "StringReader", "StringWriter",
        "SqlConnection", "SqlCommand", "SqlDataReader", "DbConnection", "DbCommand", "DbDataReader",
        "HttpClient", "WebClient", "TcpClient", "UdpClient", "Socket",
        "Process", "Timer", "FileSystemWatcher",
        "Mutex", "Semaphore", "EventWaitHandle", "ManualResetEvent", "AutoResetEvent",
        "Graphics", "Bitmap", "Image", "Font", "Brush", "Pen",
        "CryptoStream", "ICryptoTransform"
    };

    public override Task<IEnumerable<AnalysisResult>> AnalyzeAsync(
        SyntaxTree syntaxTree,
        SemanticModel? semanticModel,
        string filePath)
    {
        var results = new List<AnalysisResult>();
        var root = syntaxTree.GetRoot();

        // Check for disposable objects created without using statement
        var objectCreations = root.DescendantNodes().OfType<ObjectCreationExpressionSyntax>();

        foreach (var creation in objectCreations)
        {
            var typeName = creation.Type.ToString();

            if (IsDisposableType(typeName))
            {
                // Check if inside using statement
                if (IsInsideUsingStatement(creation))
                    continue;

                // Check if assigned to a field (might be disposed elsewhere)
                if (IsFieldAssignment(creation))
                    continue;

                // Check if Dispose is called in the same method
                if (IsDisposedInSameMethod(creation, typeName))
                    continue;

                // Check if returned from method
                if (IsReturnedFromMethod(creation))
                    continue;

                // Check if passed to another method that takes ownership
                if (IsPassedToOwnershipMethod(creation))
                    continue;

                results.Add(CreateResult(
                    "REL002",
                    "Potential Resource Leak",
                    $"'{typeName}' implements IDisposable but may not be properly disposed.",
                    filePath,
                    creation.GetLocation(),
                    Severity.Major,
                    GetCodeSnippet(creation),
                    "Use a 'using' statement or 'using' declaration to ensure proper disposal.",
                    "CWE-404"));
            }
        }

        // Check for variables of disposable types not in using
        var variableDeclarations = root.DescendantNodes().OfType<VariableDeclarationSyntax>();
        foreach (var declaration in variableDeclarations)
        {
            var typeName = declaration.Type.ToString();

            if (IsDisposableType(typeName))
            {
                foreach (var variable in declaration.Variables)
                {
                    if (variable.Initializer == null)
                        continue;

                    // Check if this is a using declaration (C# 8+)
                    var parent = declaration.Parent;
                    if (parent is UsingStatementSyntax || parent is LocalDeclarationStatementSyntax local &&
                        local.UsingKeyword != default)
                        continue;

                    // Check if inside using statement
                    if (declaration.Ancestors().Any(a => a is UsingStatementSyntax))
                        continue;

                    // Check if it's disposed
                    var containingMethod = declaration.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
                    if (containingMethod != null)
                    {
                        var varName = variable.Identifier.Text;
                        var disposePatterns = new[]
                        {
                            $"{varName}.Dispose()",
                            $"{varName}?.Dispose()",
                            $"{varName}.Close()",
                            $"{varName}?.Close()"
                        };

                        var methodText = containingMethod.ToString();
                        if (disposePatterns.Any(p => methodText.Contains(p)))
                            continue;
                    }

                    results.Add(CreateResult(
                        "REL002",
                        "Potential Resource Leak",
                        $"Variable '{variable.Identifier.Text}' of disposable type '{typeName}' may not be disposed.",
                        filePath,
                        variable.GetLocation(),
                        Severity.Major,
                        GetCodeSnippet(declaration),
                        "Use 'using var' declaration or wrap in 'using' statement.",
                        "CWE-404"));
                }
            }
        }

        // Check for async disposal issues
        var awaitExpressions = root.DescendantNodes().OfType<AwaitExpressionSyntax>();
        foreach (var await in awaitExpressions)
        {
            if (await.Expression is InvocationExpressionSyntax invocation)
            {
                var methodName = invocation.Expression.ToString();
                if (methodName.EndsWith("Async") &&
                    IsDisposableType(methodName.Replace("Async", "")))
                {
                    // Check if using await using
                    var parent = await.Parent;
                    if (!(parent is UsingStatementSyntax) &&
                        !await.Ancestors().Any(a => a is UsingStatementSyntax))
                    {
                        results.Add(CreateResult(
                            "REL002",
                            "Async Disposable Not Properly Disposed",
                            "Async operation returning disposable type may not be properly disposed.",
                            filePath,
                            await.GetLocation(),
                            Severity.Major,
                            GetCodeSnippet(await),
                            "Use 'await using' for async disposable resources.",
                            "CWE-404"));
                    }
                }
            }
        }

        return Task.FromResult<IEnumerable<AnalysisResult>>(results);
    }

    private static bool IsDisposableType(string typeName)
    {
        return DisposableTypes.Any(d =>
            typeName.Equals(d, StringComparison.OrdinalIgnoreCase) ||
            typeName.EndsWith(d, StringComparison.OrdinalIgnoreCase) ||
            typeName.Contains($"<{d}>") ||
            typeName.StartsWith($"{d}<"));
    }

    private static bool IsInsideUsingStatement(SyntaxNode node)
    {
        return node.Ancestors().Any(a =>
            a is UsingStatementSyntax ||
            (a is LocalDeclarationStatementSyntax local && local.UsingKeyword != default));
    }

    private static bool IsFieldAssignment(ObjectCreationExpressionSyntax creation)
    {
        var parent = creation.Parent;

        if (parent is EqualsValueClauseSyntax equals)
        {
            var fieldDecl = equals.Ancestors().OfType<FieldDeclarationSyntax>().FirstOrDefault();
            if (fieldDecl != null)
                return true;
        }

        if (parent is AssignmentExpressionSyntax assignment)
        {
            // Check if assigning to a field (this.field or _field pattern)
            var target = assignment.Left.ToString();
            return target.StartsWith("this.") || target.StartsWith("_");
        }

        return false;
    }

    private static bool IsDisposedInSameMethod(ObjectCreationExpressionSyntax creation, string typeName)
    {
        var method = creation.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
        if (method == null)
            return false;

        var methodBody = method.Body?.ToString() ?? method.ExpressionBody?.ToString() ?? "";

        // Check for try-finally with Dispose
        var tryStatements = method.DescendantNodes().OfType<TryStatementSyntax>();
        foreach (var tryStmt in tryStatements)
        {
            if (tryStmt.Finally != null)
            {
                var finallyText = tryStmt.Finally.ToString();
                if (finallyText.Contains("Dispose") || finallyText.Contains("Close"))
                    return true;
            }
        }

        return false;
    }

    private static bool IsReturnedFromMethod(ObjectCreationExpressionSyntax creation)
    {
        var parent = creation.Parent;
        while (parent != null)
        {
            if (parent is ReturnStatementSyntax)
                return true;
            if (parent is ArrowExpressionClauseSyntax)
                return true;
            if (parent is LambdaExpressionSyntax)
                return true;

            parent = parent.Parent;
        }
        return false;
    }

    private static bool IsPassedToOwnershipMethod(ObjectCreationExpressionSyntax creation)
    {
        var parent = creation.Parent;
        if (parent is ArgumentSyntax)
            return true; // Passed as argument, ownership may transfer

        return false;
    }
}
