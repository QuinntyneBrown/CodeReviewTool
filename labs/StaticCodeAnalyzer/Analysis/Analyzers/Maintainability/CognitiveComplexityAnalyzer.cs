using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StaticCodeAnalyzer.Analysis.Analyzers.Maintainability;

public class CognitiveComplexityAnalyzer : BaseCodeAnalyzer
{
    public override string AnalyzerId => "MAINT006";
    public override string Name => "Cognitive Complexity Analyzer";
    public override IssueCategory Category => IssueCategory.Maintainability;

    private const int WarningThreshold = 15;
    private const int CriticalThreshold = 25;
    private const int BlockerThreshold = 40;

    public override Task<IEnumerable<AnalysisResult>> AnalyzeAsync(
        SyntaxTree syntaxTree,
        SemanticModel? semanticModel,
        string filePath)
    {
        var results = new List<AnalysisResult>();
        var root = syntaxTree.GetRoot();

        var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();

        foreach (var method in methods)
        {
            var complexity = CalculateCognitiveComplexity(method);
            var methodName = method.Identifier.Text;

            if (complexity >= BlockerThreshold)
            {
                results.Add(CreateResult(
                    "MAINT006",
                    "Extreme Cognitive Complexity",
                    $"Method '{methodName}' has cognitive complexity of {complexity}. This is very hard to understand.",
                    filePath,
                    method.Identifier.GetLocation(),
                    Severity.Blocker,
                    $"{methodName}() - Cognitive Complexity: {complexity}",
                    "Break this method into smaller, more understandable pieces."));
            }
            else if (complexity >= CriticalThreshold)
            {
                results.Add(CreateResult(
                    "MAINT006",
                    "Very High Cognitive Complexity",
                    $"Method '{methodName}' has cognitive complexity of {complexity}. Maximum recommended is {WarningThreshold}.",
                    filePath,
                    method.Identifier.GetLocation(),
                    Severity.Critical,
                    $"{methodName}() - Cognitive Complexity: {complexity}",
                    "Simplify this method by extracting logic into well-named helper methods."));
            }
            else if (complexity >= WarningThreshold)
            {
                results.Add(CreateResult(
                    "MAINT006",
                    "High Cognitive Complexity",
                    $"Method '{methodName}' has cognitive complexity of {complexity}. Maximum recommended is {WarningThreshold}.",
                    filePath,
                    method.Identifier.GetLocation(),
                    Severity.Major,
                    $"{methodName}() - Cognitive Complexity: {complexity}",
                    "Consider refactoring to improve readability."));
            }
        }

        return Task.FromResult<IEnumerable<AnalysisResult>>(results);
    }

    private static int CalculateCognitiveComplexity(MethodDeclarationSyntax method)
    {
        if (method.Body == null && method.ExpressionBody == null)
            return 0;

        int complexity = 0;
        var body = method.Body ?? (SyntaxNode?)method.ExpressionBody;

        if (body != null)
        {
            complexity = CalculateNodeComplexity(body, 0);
        }

        return complexity;
    }

    private static int CalculateNodeComplexity(SyntaxNode node, int nestingLevel)
    {
        int complexity = 0;

        foreach (var child in node.ChildNodes())
        {
            int increment = 0;
            int newNestingLevel = nestingLevel;

            switch (child)
            {
                // Structural complexity increments (also add nesting penalty)
                case IfStatementSyntax ifStmt:
                    increment = 1 + nestingLevel;
                    newNestingLevel = nestingLevel + 1;

                    // Check for else if (doesn't add to nesting)
                    if (ifStmt.Else?.Statement is IfStatementSyntax)
                    {
                        // else-if is a linear addition, not nested
                        complexity += increment;
                        complexity += CalculateNodeComplexity(ifStmt.Condition, nestingLevel);
                        complexity += CalculateNodeComplexity(ifStmt.Statement, newNestingLevel);
                        complexity += CalculateNodeComplexity(ifStmt.Else.Statement, nestingLevel);
                        continue;
                    }
                    break;

                case ForStatementSyntax:
                case ForEachStatementSyntax:
                case WhileStatementSyntax:
                case DoStatementSyntax:
                    increment = 1 + nestingLevel;
                    newNestingLevel = nestingLevel + 1;
                    break;

                case SwitchStatementSyntax:
                    increment = 1 + nestingLevel;
                    newNestingLevel = nestingLevel + 1;
                    break;

                case CatchClauseSyntax:
                    increment = 1 + nestingLevel;
                    newNestingLevel = nestingLevel + 1;
                    break;

                case ConditionalExpressionSyntax:
                    increment = 1 + nestingLevel;
                    break;

                // Fundamental increments (no nesting penalty)
                case GotoStatementSyntax:
                case BreakStatementSyntax when !IsInSwitch(child):
                case ContinueStatementSyntax:
                    increment = 1;
                    break;

                // Binary logical operators
                case BinaryExpressionSyntax binary:
                    if (binary.IsKind(SyntaxKind.LogicalAndExpression) ||
                        binary.IsKind(SyntaxKind.LogicalOrExpression))
                    {
                        // Count sequences of same operator as 1
                        if (!IsSameOperatorAsParent(binary))
                        {
                            increment = 1;
                        }
                    }
                    break;

                // Lambda expressions add nesting
                case LambdaExpressionSyntax:
                    newNestingLevel = nestingLevel + 1;
                    break;

                // Recursive calls
                case InvocationExpressionSyntax invocation:
                    var containingMethod = node.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
                    if (containingMethod != null)
                    {
                        var methodName = GetMethodName(invocation);
                        if (methodName == containingMethod.Identifier.Text)
                        {
                            increment = 1;
                        }
                    }
                    break;

                // Null coalescing
                case BinaryExpressionSyntax coalesce when coalesce.IsKind(SyntaxKind.CoalesceExpression):
                    increment = 1;
                    break;
            }

            complexity += increment;
            complexity += CalculateNodeComplexity(child, newNestingLevel);
        }

        return complexity;
    }

    private static bool IsInSwitch(SyntaxNode node)
    {
        return node.Ancestors().Any(a => a is SwitchStatementSyntax);
    }

    private static bool IsSameOperatorAsParent(BinaryExpressionSyntax binary)
    {
        if (binary.Parent is BinaryExpressionSyntax parent)
        {
            return (binary.IsKind(SyntaxKind.LogicalAndExpression) && parent.IsKind(SyntaxKind.LogicalAndExpression)) ||
                   (binary.IsKind(SyntaxKind.LogicalOrExpression) && parent.IsKind(SyntaxKind.LogicalOrExpression));
        }
        return false;
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
}
