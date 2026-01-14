using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StaticCodeAnalyzer.Analysis.Analyzers.Reliability;

public class NullReferenceAnalyzer : BaseCodeAnalyzer
{
    public override string AnalyzerId => "REL001";
    public override string Name => "Null Reference Analyzer";
    public override IssueCategory Category => IssueCategory.Reliability;

    public override Task<IEnumerable<AnalysisResult>> AnalyzeAsync(
        SyntaxTree syntaxTree,
        SemanticModel? semanticModel,
        string filePath)
    {
        var results = new List<AnalysisResult>();
        var root = syntaxTree.GetRoot();

        // Find potential null dereferences after null checks
        var ifStatements = root.DescendantNodes().OfType<IfStatementSyntax>();
        foreach (var ifStmt in ifStatements)
        {
            // Check for null check pattern
            var condition = ifStmt.Condition.ToString();
            if (condition.Contains("== null") || condition.Contains("is null"))
            {
                // Extract variable being checked
                var variable = ExtractNullCheckedVariable(ifStmt.Condition);
                if (!string.IsNullOrEmpty(variable))
                {
                    // Check if variable is used without null handling in else branch
                    if (ifStmt.Else != null)
                    {
                        var elseUsages = ifStmt.Else.DescendantNodes()
                            .OfType<MemberAccessExpressionSyntax>()
                            .Where(m => m.Expression.ToString() == variable);

                        // This is actually okay - in else branch the value is NOT null
                    }
                }
            }

            // Check for inverted null check without proper handling
            if (condition.Contains("!= null") || condition.Contains("is not null"))
            {
                var variable = ExtractNullCheckedVariable(ifStmt.Condition);
                if (!string.IsNullOrEmpty(variable))
                {
                    // Variable used after the if without null check
                    var parentBlock = ifStmt.Parent as BlockSyntax;
                    if (parentBlock != null && ifStmt.Else == null)
                    {
                        var statementsAfterIf = parentBlock.Statements
                            .SkipWhile(s => s != ifStmt)
                            .Skip(1);

                        // Check if there's a return/throw in the if block
                        bool ifBlockReturns = ifStmt.Statement.DescendantNodes()
                            .Any(n => n is ReturnStatementSyntax || n is ThrowStatementSyntax);

                        if (!ifBlockReturns)
                        {
                            foreach (var stmt in statementsAfterIf)
                            {
                                var usages = stmt.DescendantNodes()
                                    .OfType<MemberAccessExpressionSyntax>()
                                    .Where(m => m.Expression.ToString() == variable &&
                                               !IsNullConditionalAccess(m));

                                foreach (var usage in usages)
                                {
                                    results.Add(CreateResult(
                                        "REL001",
                                        "Potential Null Dereference",
                                        $"Variable '{variable}' may be null when accessed here. The null check doesn't guarantee safety for subsequent code.",
                                        filePath,
                                        usage.GetLocation(),
                                        Severity.Major,
                                        GetCodeSnippet(usage),
                                        "Ensure the variable is not null before accessing members, or use null-conditional operator (?.).",
                                        "CWE-476"));
                                }
                            }
                        }
                    }
                }
            }
        }

        // Check for direct member access on potentially null values
        var memberAccesses = root.DescendantNodes().OfType<MemberAccessExpressionSyntax>();
        foreach (var access in memberAccesses)
        {
            // Check for access on method return that might be null
            if (access.Expression is InvocationExpressionSyntax invocation)
            {
                var methodName = invocation.Expression.ToString().ToLowerInvariant();

                // Methods that commonly return null
                var nullableMethods = new[]
                {
                    "find", "firstordefault", "singleordefault", "lastordefault",
                    "getvalue", "get", "elementatordefault"
                };

                if (nullableMethods.Any(m => methodName.Contains(m)) &&
                    !IsNullConditionalAccess(access))
                {
                    results.Add(CreateResult(
                        "REL001",
                        "Potential Null Dereference on Method Return",
                        $"Method '{GetMethodName(invocation)}' may return null. Direct member access without null check.",
                        filePath,
                        access.GetLocation(),
                        Severity.Major,
                        GetCodeSnippet(access),
                        "Use null-conditional operator (?.) or check for null before accessing members.",
                        "CWE-476"));
                }
            }

            // Check for access on cast results
            if (access.Expression is CastExpressionSyntax ||
                (access.Expression is BinaryExpressionSyntax binary && binary.IsKind(SyntaxKind.AsExpression)))
            {
                if (!IsNullConditionalAccess(access))
                {
                    results.Add(CreateResult(
                        "REL001",
                        "Potential Null Dereference After Cast",
                        "Type cast with 'as' may return null. Direct member access without null check.",
                        filePath,
                        access.GetLocation(),
                        Severity.Major,
                        GetCodeSnippet(access),
                        "Check for null after 'as' cast, or use pattern matching 'is Type variable'.",
                        "CWE-476"));
                }
            }
        }

        // Check for nullable value type dereference without .Value check
        var conditionalAccesses = root.DescendantNodes().OfType<ConditionalAccessExpressionSyntax>();
        // These are generally safe, but note patterns

        // Check for array/collection access after nullable check
        var elementAccesses = root.DescendantNodes().OfType<ElementAccessExpressionSyntax>();
        foreach (var access in elementAccesses)
        {
            var expr = access.Expression.ToString();
            if (expr.EndsWith("?"))
            {
                // Null conditional, safe
                continue;
            }

            // Check if expression is a potentially null value
            if (access.Expression is IdentifierNameSyntax identifier)
            {
                // Try to find if this was checked for null
                var containingMethod = access.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
                if (containingMethod != null)
                {
                    // Simple check - is there a null check earlier?
                    var precedingStatements = containingMethod.DescendantNodes()
                        .TakeWhile(n => n != access)
                        .OfType<IfStatementSyntax>()
                        .Where(i => i.Condition.ToString().Contains(identifier.Identifier.Text) &&
                                   (i.Condition.ToString().Contains("null") ||
                                    i.Condition.ToString().Contains("Count") ||
                                    i.Condition.ToString().Contains("Length")));

                    // This is a heuristic check
                }
            }
        }

        return Task.FromResult<IEnumerable<AnalysisResult>>(results);
    }

    private static string? ExtractNullCheckedVariable(ExpressionSyntax condition)
    {
        if (condition is BinaryExpressionSyntax binary)
        {
            if (binary.Right is LiteralExpressionSyntax literal &&
                literal.IsKind(SyntaxKind.NullLiteralExpression))
            {
                return binary.Left.ToString();
            }
            if (binary.Left is LiteralExpressionSyntax leftLiteral &&
                leftLiteral.IsKind(SyntaxKind.NullLiteralExpression))
            {
                return binary.Right.ToString();
            }
        }

        if (condition is IsPatternExpressionSyntax isPattern)
        {
            return isPattern.Expression.ToString();
        }

        if (condition is PrefixUnaryExpressionSyntax prefix &&
            prefix.IsKind(SyntaxKind.LogicalNotExpression))
        {
            return ExtractNullCheckedVariable(prefix.Operand);
        }

        return null;
    }

    private static bool IsNullConditionalAccess(MemberAccessExpressionSyntax access)
    {
        // Check if parent is conditional access
        return access.Parent is ConditionalAccessExpressionSyntax ||
               access.Expression.ToString().EndsWith("?");
    }

    private static string GetMethodName(InvocationExpressionSyntax invocation)
    {
        return invocation.Expression switch
        {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.Text,
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            _ => invocation.Expression.ToString()
        };
    }
}
