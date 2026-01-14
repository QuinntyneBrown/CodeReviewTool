using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StaticCodeAnalyzer.Analysis.Analyzers.Reliability;

public class ExceptionHandlingAnalyzer : BaseCodeAnalyzer
{
    public override string AnalyzerId => "REL004";
    public override string Name => "Exception Handling Analyzer";
    public override IssueCategory Category => IssueCategory.Reliability;

    private static readonly HashSet<string> SystemExceptions = new()
    {
        "OutOfMemoryException", "StackOverflowException", "ExecutionEngineException",
        "AccessViolationException", "ThreadAbortException"
    };

    private static readonly HashSet<string> SecurityExceptions = new()
    {
        "SecurityException", "UnauthorizedAccessException"
    };

    public override Task<IEnumerable<AnalysisResult>> AnalyzeAsync(
        SyntaxTree syntaxTree,
        SemanticModel? semanticModel,
        string filePath)
    {
        var results = new List<AnalysisResult>();
        var root = syntaxTree.GetRoot();

        var catchClauses = root.DescendantNodes().OfType<CatchClauseSyntax>();

        foreach (var catchClause in catchClauses)
        {
            var exceptionType = catchClause.Declaration?.Type?.ToString() ?? "";

            // Check for catching system-critical exceptions
            foreach (var sysEx in SystemExceptions)
            {
                if (exceptionType.Contains(sysEx))
                {
                    results.Add(CreateResult(
                        "REL004",
                        "Catching System-Critical Exception",
                        $"Catching '{sysEx}' is generally not recommended as it indicates serious system problems.",
                        filePath,
                        catchClause.GetLocation(),
                        Severity.Critical,
                        GetCodeSnippet(catchClause),
                        "Let system-critical exceptions propagate. Handle at application boundaries only.",
                        "CWE-396"));
                }
            }

            // Check for security exceptions being swallowed
            foreach (var secEx in SecurityExceptions)
            {
                if (exceptionType.Contains(secEx))
                {
                    var block = catchClause.Block;
                    if (!block.Statements.Any(s =>
                        s is ThrowStatementSyntax ||
                        s.ToString().Contains("throw")))
                    {
                        results.Add(CreateResult(
                            "REL004",
                            "Security Exception Swallowed",
                            $"'{secEx}' is caught but not rethrown. This may hide security violations.",
                            filePath,
                            catchClause.GetLocation(),
                            Severity.Critical,
                            GetCodeSnippet(catchClause),
                            "Log security exceptions and rethrow or handle appropriately.",
                            "CWE-755"));
                    }
                }
            }
        }

        // Check for exception thrown in finally block
        var finallyBlocks = root.DescendantNodes().OfType<FinallyClauseSyntax>();
        foreach (var finallyBlock in finallyBlocks)
        {
            var throwStatements = finallyBlock.DescendantNodes().OfType<ThrowStatementSyntax>();
            foreach (var throwStmt in throwStatements)
            {
                results.Add(CreateResult(
                    "REL004",
                    "Throw in Finally Block",
                    "Throwing an exception in a finally block can mask the original exception.",
                    filePath,
                    throwStmt.GetLocation(),
                    Severity.Major,
                    GetCodeSnippet(throwStmt),
                    "Avoid throwing exceptions in finally blocks. Handle cleanup errors without throwing.",
                    "CWE-460"));
            }

            // Check for risky operations in finally
            var invocations = finallyBlock.DescendantNodes().OfType<InvocationExpressionSyntax>();
            foreach (var invocation in invocations)
            {
                var methodName = invocation.Expression.ToString();
                // These are generally safe in finally
                if (!methodName.Contains("Dispose") && !methodName.Contains("Close") &&
                    !methodName.Contains("Unlock") && !methodName.Contains("Release"))
                {
                    // Check for operations that might throw
                    if (methodName.Contains("Read") || methodName.Contains("Write") ||
                        methodName.Contains("Execute") || methodName.Contains("Send"))
                    {
                        results.Add(CreateResult(
                            "REL004",
                            "Risky Operation in Finally Block",
                            "Operations that may throw exceptions should be avoided in finally blocks.",
                            filePath,
                            invocation.GetLocation(),
                            Severity.Minor,
                            GetCodeSnippet(invocation),
                            "Move potentially failing operations out of finally block, or wrap in try-catch.",
                            "CWE-460"));
                    }
                }
            }
        }

        // Check for return in finally
        foreach (var finallyBlock in finallyBlocks)
        {
            var returnStatements = finallyBlock.DescendantNodes().OfType<ReturnStatementSyntax>();
            foreach (var returnStmt in returnStatements)
            {
                results.Add(CreateResult(
                    "REL004",
                    "Return in Finally Block",
                    "Return in finally block can override the return value from try block.",
                    filePath,
                    returnStmt.GetLocation(),
                    Severity.Major,
                    GetCodeSnippet(returnStmt),
                    "Move return statement out of finally block.",
                    "CWE-584"));
            }
        }

        // Check for try blocks with no catch or finally
        var tryStatements = root.DescendantNodes().OfType<TryStatementSyntax>();
        foreach (var tryStmt in tryStatements)
        {
            if (!tryStmt.Catches.Any() && tryStmt.Finally == null)
            {
                results.Add(CreateResult(
                    "REL004",
                    "Try Block Without Catch or Finally",
                    "Try block has neither catch nor finally clause. This is likely incomplete.",
                    filePath,
                    tryStmt.GetLocation(),
                    Severity.Major,
                    GetCodeSnippet(tryStmt),
                    "Add catch or finally clause to handle or cleanup after exceptions.",
                    "CWE-390"));
            }
        }

        // Check for catching and re-throwing different exception in problematic ways
        foreach (var catchClause in catchClauses)
        {
            var throwStatements = catchClause.Block.DescendantNodes().OfType<ThrowStatementSyntax>();

            foreach (var throwStmt in throwStatements)
            {
                if (throwStmt.Expression is ObjectCreationExpressionSyntax creation)
                {
                    var newExceptionType = creation.Type.ToString();

                    // Check for wrapping with less specific exception
                    if (newExceptionType == "Exception" || newExceptionType == "System.Exception")
                    {
                        results.Add(CreateResult(
                            "REL004",
                            "Re-throwing Generic Exception",
                            "Wrapping caught exception in generic 'Exception' loses type information.",
                            filePath,
                            throwStmt.GetLocation(),
                            Severity.Minor,
                            GetCodeSnippet(throwStmt),
                            "Use more specific exception types when wrapping exceptions.",
                            "CWE-397"));
                    }
                }
            }
        }

        return Task.FromResult<IEnumerable<AnalysisResult>>(results);
    }
}
