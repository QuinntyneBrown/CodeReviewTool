using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StaticCodeAnalyzer.Analysis.Analyzers.Reliability;

public class ThreadSafetyAnalyzer : BaseCodeAnalyzer
{
    public override string AnalyzerId => "REL005";
    public override string Name => "Thread Safety Analyzer";
    public override IssueCategory Category => IssueCategory.Reliability;

    private static readonly HashSet<string> NonThreadSafeTypes = new()
    {
        "List", "Dictionary", "HashSet", "Queue", "Stack",
        "ArrayList", "Hashtable", "SortedList", "SortedDictionary",
        "LinkedList", "StringBuilder"
    };

    private static readonly HashSet<string> ThreadUnsafePatterns = new()
    {
        "DateTime.Now", "DateTime.UtcNow", "Random"
    };

    public override Task<IEnumerable<AnalysisResult>> AnalyzeAsync(
        SyntaxTree syntaxTree,
        SemanticModel? semanticModel,
        string filePath)
    {
        var results = new List<AnalysisResult>();
        var root = syntaxTree.GetRoot();

        // Find static fields with non-thread-safe types
        var fieldDeclarations = root.DescendantNodes().OfType<FieldDeclarationSyntax>();
        foreach (var field in fieldDeclarations)
        {
            bool isStatic = field.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword));
            bool isReadonly = field.Modifiers.Any(m => m.IsKind(SyntaxKind.ReadOnlyKeyword));

            if (isStatic && !isReadonly)
            {
                var typeName = field.Declaration.Type.ToString();

                if (NonThreadSafeTypes.Any(t => typeName.Contains(t)))
                {
                    results.Add(CreateResult(
                        "REL005",
                        "Non-Thread-Safe Static Field",
                        $"Static field of type '{typeName}' is not thread-safe for concurrent access.",
                        filePath,
                        field.GetLocation(),
                        Severity.Major,
                        GetCodeSnippet(field),
                        "Use Concurrent collections (ConcurrentDictionary, ConcurrentQueue) or add synchronization.",
                        "CWE-362"));
                }
            }
        }

        // Check for lock on non-readonly field
        var lockStatements = root.DescendantNodes().OfType<LockStatementSyntax>();
        foreach (var lockStmt in lockStatements)
        {
            var lockExpr = lockStmt.Expression;

            // Check for lock(this)
            if (lockExpr is ThisExpressionSyntax)
            {
                results.Add(CreateResult(
                    "REL005",
                    "Lock on 'this'",
                    "Locking on 'this' is not recommended as external code could also lock on this instance.",
                    filePath,
                    lockStmt.GetLocation(),
                    Severity.Major,
                    GetCodeSnippet(lockStmt),
                    "Use a private readonly object for locking: private readonly object _lock = new object();",
                    "CWE-764"));
            }

            // Check for lock on type
            if (lockExpr is TypeOfExpressionSyntax)
            {
                results.Add(CreateResult(
                    "REL005",
                    "Lock on Type",
                    "Locking on a Type object is not recommended as it's publicly accessible.",
                    filePath,
                    lockStmt.GetLocation(),
                    Severity.Major,
                    GetCodeSnippet(lockStmt),
                    "Use a private static readonly object for locking.",
                    "CWE-764"));
            }

            // Check for lock on string
            if (lockExpr is LiteralExpressionSyntax literal &&
                literal.IsKind(SyntaxKind.StringLiteralExpression))
            {
                results.Add(CreateResult(
                    "REL005",
                    "Lock on String Literal",
                    "Locking on a string literal can cause deadlocks due to string interning.",
                    filePath,
                    lockStmt.GetLocation(),
                    Severity.Critical,
                    GetCodeSnippet(lockStmt),
                    "Use a dedicated private readonly object for locking.",
                    "CWE-833"));
            }
        }

        // Check for double-checked locking without volatile
        foreach (var lockStmt in lockStatements)
        {
            var parentIf = lockStmt.Parent as BlockSyntax;
            if (parentIf?.Parent is IfStatementSyntax outerIf)
            {
                // Check if there's an if inside the lock with same condition
                var innerIfs = lockStmt.Statement.DescendantNodes().OfType<IfStatementSyntax>();
                foreach (var innerIf in innerIfs)
                {
                    if (AreConditionsSimilar(outerIf.Condition, innerIf.Condition))
                    {
                        results.Add(CreateResult(
                            "REL005",
                            "Double-Checked Locking Pattern",
                            "Double-checked locking detected. Ensure the field is volatile or use Lazy<T>.",
                            filePath,
                            lockStmt.GetLocation(),
                            Severity.Major,
                            GetCodeSnippet(lockStmt),
                            "Use Lazy<T> for lazy initialization, or mark the field as volatile.",
                            "CWE-609"));
                    }
                }
            }
        }

        // Check for async void methods (except event handlers)
        var methodDeclarations = root.DescendantNodes().OfType<MethodDeclarationSyntax>();
        foreach (var method in methodDeclarations)
        {
            if (method.Modifiers.Any(m => m.IsKind(SyntaxKind.AsyncKeyword)) &&
                method.ReturnType.ToString() == "void")
            {
                // Check if it's an event handler pattern
                var parameters = method.ParameterList.Parameters;
                bool isEventHandler = parameters.Count == 2 &&
                    parameters[0].Type?.ToString() == "object" &&
                    parameters[1].Type?.ToString().EndsWith("EventArgs") == true;

                if (!isEventHandler)
                {
                    results.Add(CreateResult(
                        "REL005",
                        "Async Void Method",
                        "Async void methods cannot be awaited and exceptions cannot be caught by the caller.",
                        filePath,
                        method.GetLocation(),
                        Severity.Major,
                        $"async void {method.Identifier.Text}(...)",
                        "Change return type to Task. Async void should only be used for event handlers.",
                        "CWE-755"));
                }
            }
        }

        // Check for shared state access in async context without synchronization
        var awaitExpressions = root.DescendantNodes().OfType<AwaitExpressionSyntax>();
        foreach (var awaitExpr in awaitExpressions)
        {
            var containingMethod = awaitExpr.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
            if (containingMethod != null)
            {
                // Check for field access after await
                var statementsAfterAwait = GetStatementsAfter(awaitExpr, containingMethod);
                foreach (var stmt in statementsAfterAwait)
                {
                    var fieldAccesses = stmt.DescendantNodes().OfType<MemberAccessExpressionSyntax>()
                        .Where(m => m.Expression is ThisExpressionSyntax ||
                                   m.Expression.ToString().StartsWith("_"));

                    // This is a heuristic - actual thread safety analysis is more complex
                }
            }
        }

        return Task.FromResult<IEnumerable<AnalysisResult>>(results);
    }

    private static bool AreConditionsSimilar(ExpressionSyntax a, ExpressionSyntax b)
    {
        // Simple textual comparison
        return a.ToString().Trim() == b.ToString().Trim();
    }

    private static IEnumerable<StatementSyntax> GetStatementsAfter(SyntaxNode node, MethodDeclarationSyntax method)
    {
        var block = method.Body;
        if (block == null)
            return Enumerable.Empty<StatementSyntax>();

        bool found = false;
        foreach (var stmt in block.Statements)
        {
            if (found)
                yield return stmt;

            if (stmt.Contains(node))
                found = true;
        }
    }
}
