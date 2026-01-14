using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StaticCodeAnalyzer.Analysis.Analyzers.Security;

public class SqlInjectionAnalyzer : BaseCodeAnalyzer
{
    public override string AnalyzerId => "SEC001";
    public override string Name => "SQL Injection Analyzer";
    public override IssueCategory Category => IssueCategory.Security;

    private static readonly HashSet<string> SqlExecutionMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "ExecuteNonQuery", "ExecuteReader", "ExecuteScalar",
        "ExecuteNonQueryAsync", "ExecuteReaderAsync", "ExecuteScalarAsync",
        "FromSqlRaw", "ExecuteSqlRaw", "ExecuteSqlRawAsync",
        "SqlQuery", "Database.ExecuteSqlCommand"
    };

    private static readonly HashSet<string> DangerousStringMethods = new()
    {
        "Format", "Concat", "Join", "Replace"
    };

    public override Task<IEnumerable<AnalysisResult>> AnalyzeAsync(
        SyntaxTree syntaxTree,
        SemanticModel? semanticModel,
        string filePath)
    {
        var results = new List<AnalysisResult>();
        var root = syntaxTree.GetRoot();

        // Find string concatenation in SQL contexts
        var binaryExpressions = root.DescendantNodes().OfType<BinaryExpressionSyntax>()
            .Where(b => b.IsKind(SyntaxKind.AddExpression));

        foreach (var expr in binaryExpressions)
        {
            if (IsInSqlContext(expr) && ContainsUserInput(expr))
            {
                results.Add(CreateResult(
                    "SEC001",
                    "Potential SQL Injection",
                    "String concatenation detected in SQL query. This may allow SQL injection attacks.",
                    filePath,
                    expr.GetLocation(),
                    Severity.Critical,
                    GetCodeSnippet(expr),
                    "Use parameterized queries or stored procedures instead of string concatenation.",
                    "CWE-89",
                    "A03:2021 - Injection"));
            }
        }

        // Find string interpolation in SQL contexts
        var interpolatedStrings = root.DescendantNodes().OfType<InterpolatedStringExpressionSyntax>();
        foreach (var interpolated in interpolatedStrings)
        {
            if (IsInSqlContext(interpolated) && HasInterpolationContents(interpolated))
            {
                results.Add(CreateResult(
                    "SEC001",
                    "Potential SQL Injection via String Interpolation",
                    "String interpolation detected in SQL query. This may allow SQL injection attacks.",
                    filePath,
                    interpolated.GetLocation(),
                    Severity.Critical,
                    GetCodeSnippet(interpolated),
                    "Use parameterized queries with SqlParameter or ORM parameters.",
                    "CWE-89",
                    "A03:2021 - Injection"));
            }
        }

        // Find dangerous method calls with SQL
        var invocations = root.DescendantNodes().OfType<InvocationExpressionSyntax>();
        foreach (var invocation in invocations)
        {
            var methodName = GetMethodName(invocation);
            if (SqlExecutionMethods.Contains(methodName))
            {
                var arguments = invocation.ArgumentList.Arguments;
                foreach (var arg in arguments)
                {
                    if (IsDynamicSqlArgument(arg.Expression))
                    {
                        results.Add(CreateResult(
                            "SEC001",
                            "Potential SQL Injection in Method Call",
                            $"Dynamic SQL detected in {methodName} call. Consider using parameterized queries.",
                            filePath,
                            invocation.GetLocation(),
                            Severity.Critical,
                            GetCodeSnippet(invocation),
                            "Use parameterized queries or an ORM with proper parameter binding.",
                            "CWE-89",
                            "A03:2021 - Injection"));
                        break;
                    }
                }
            }
        }

        return Task.FromResult<IEnumerable<AnalysisResult>>(results);
    }

    private static bool IsInSqlContext(SyntaxNode node)
    {
        var text = node.ToString().ToLowerInvariant();
        var sqlKeywords = new[] { "select", "insert", "update", "delete", "from", "where", "exec", "execute" };
        return sqlKeywords.Any(kw => text.Contains(kw));
    }

    private static bool ContainsUserInput(BinaryExpressionSyntax expr)
    {
        var text = expr.ToString().ToLowerInvariant();
        var inputIndicators = new[] { "request", "input", "param", "query", "form", "user", "data" };
        return inputIndicators.Any(ind => text.Contains(ind)) ||
               expr.DescendantNodes().OfType<IdentifierNameSyntax>().Any();
    }

    private static bool HasInterpolationContents(InterpolatedStringExpressionSyntax interpolated)
    {
        return interpolated.Contents.OfType<InterpolationSyntax>().Any();
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

    private static bool IsDynamicSqlArgument(ExpressionSyntax expression)
    {
        return expression is BinaryExpressionSyntax ||
               expression is InterpolatedStringExpressionSyntax ||
               (expression is InvocationExpressionSyntax inv &&
                DangerousStringMethods.Contains(GetMethodName(inv)));
    }
}
