using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StaticCodeAnalyzer.Analysis.Analyzers.Reliability;

public class EmptyCatchBlockAnalyzer : BaseCodeAnalyzer
{
    public override string AnalyzerId => "REL003";
    public override string Name => "Empty Catch Block Analyzer";
    public override IssueCategory Category => IssueCategory.Reliability;

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
            var block = catchClause.Block;

            // Check for completely empty catch block
            if (block.Statements.Count == 0)
            {
                var exceptionType = catchClause.Declaration?.Type?.ToString() ?? "Exception";

                results.Add(CreateResult(
                    "REL003",
                    "Empty Catch Block",
                    $"Catch block for '{exceptionType}' is empty. Exceptions are being silently swallowed.",
                    filePath,
                    catchClause.GetLocation(),
                    Severity.Major,
                    GetCodeSnippet(catchClause),
                    "At minimum, log the exception. Consider if the exception should be handled differently or rethrown.",
                    "CWE-390"));
            }
            // Check for catch block with only a comment
            else if (block.Statements.All(s => ContainsOnlyComments(s)))
            {
                results.Add(CreateResult(
                    "REL003",
                    "Catch Block With Only Comments",
                    "Catch block contains only comments. Exception handling code may be missing.",
                    filePath,
                    catchClause.GetLocation(),
                    Severity.Minor,
                    GetCodeSnippet(catchClause),
                    "Add proper exception handling or logging.",
                    "CWE-390"));
            }
        }

        // Check for generic catch-all exception handlers
        foreach (var catchClause in catchClauses)
        {
            var declaration = catchClause.Declaration;

            // Bare catch clause (catch without exception type)
            if (declaration == null)
            {
                results.Add(CreateResult(
                    "REL003",
                    "Bare Catch Clause",
                    "Bare 'catch' clause catches all exceptions including system exceptions.",
                    filePath,
                    catchClause.GetLocation(),
                    Severity.Major,
                    GetCodeSnippet(catchClause),
                    "Catch specific exception types. Avoid catching all exceptions.",
                    "CWE-396"));
                continue;
            }

            var exceptionType = declaration.Type?.ToString();

            // Catching System.Exception or Exception
            if (exceptionType == "Exception" || exceptionType == "System.Exception")
            {
                // Check if this is the only catch clause (not part of a chain)
                var tryStatement = catchClause.Parent as TryStatementSyntax;
                if (tryStatement != null && tryStatement.Catches.Count == 1)
                {
                    results.Add(CreateResult(
                        "REL003",
                        "Catching Generic Exception",
                        "Catching 'Exception' type catches all exceptions, which may hide bugs.",
                        filePath,
                        catchClause.GetLocation(),
                        Severity.Minor,
                        GetCodeSnippet(catchClause),
                        "Catch more specific exception types. Only catch exceptions you can handle.",
                        "CWE-396"));
                }
            }
        }

        // Check for catch blocks that only rethrow
        foreach (var catchClause in catchClauses)
        {
            var block = catchClause.Block;

            if (block.Statements.Count == 1 &&
                block.Statements[0] is ThrowStatementSyntax throwStmt &&
                throwStmt.Expression == null)
            {
                results.Add(CreateResult(
                    "REL003",
                    "Redundant Catch Block",
                    "Catch block only contains 'throw;' which is redundant unless there's additional handling.",
                    filePath,
                    catchClause.GetLocation(),
                    Severity.Minor,
                    GetCodeSnippet(catchClause),
                    "Add meaningful exception handling or remove the try-catch if not needed.",
                    "CWE-390"));
            }
        }

        // Check for catch blocks that throw new exception losing original stack trace
        foreach (var catchClause in catchClauses)
        {
            var throwStatements = catchClause.Block.DescendantNodes().OfType<ThrowStatementSyntax>();

            foreach (var throwStmt in throwStatements)
            {
                if (throwStmt.Expression != null)
                {
                    var exceptionName = catchClause.Declaration?.Identifier.Text;

                    // Check if throwing a new exception without preserving inner exception
                    if (throwStmt.Expression is ObjectCreationExpressionSyntax creation)
                    {
                        var args = creation.ArgumentList?.Arguments;

                        // Check if inner exception is passed
                        if (args == null || !args.Value.Any(a =>
                            a.Expression.ToString() == exceptionName ||
                            a.Expression.ToString().Contains("innerException") ||
                            a.Expression.ToString().Contains("inner")))
                        {
                            results.Add(CreateResult(
                                "REL003",
                                "Exception Thrown Without Inner Exception",
                                "New exception thrown without preserving the original exception as inner exception.",
                                filePath,
                                throwStmt.GetLocation(),
                                Severity.Minor,
                                GetCodeSnippet(throwStmt),
                                $"Pass the caught exception as the inner exception: new SomeException(\"message\", {exceptionName})",
                                "CWE-390"));
                        }
                    }

                    // Check if rethrowing the exception variable (loses stack trace)
                    if (throwStmt.Expression is IdentifierNameSyntax identifier &&
                        identifier.Identifier.Text == exceptionName)
                    {
                        results.Add(CreateResult(
                            "REL003",
                            "Exception Rethrow Loses Stack Trace",
                            $"'throw {exceptionName};' resets the stack trace. Use 'throw;' to preserve it.",
                            filePath,
                            throwStmt.GetLocation(),
                            Severity.Major,
                            GetCodeSnippet(throwStmt),
                            "Use 'throw;' without the exception variable to preserve the stack trace.",
                            "CWE-390"));
                    }
                }
            }
        }

        return Task.FromResult<IEnumerable<AnalysisResult>>(results);
    }

    private static bool ContainsOnlyComments(StatementSyntax statement)
    {
        // Check if statement is empty or contains only trivia
        var text = statement.ToString().Trim();
        return string.IsNullOrEmpty(text) ||
               text.StartsWith("//") ||
               text.StartsWith("/*");
    }
}
