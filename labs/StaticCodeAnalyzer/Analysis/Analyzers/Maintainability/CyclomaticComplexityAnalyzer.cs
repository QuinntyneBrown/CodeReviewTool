using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StaticCodeAnalyzer.Analysis.Analyzers.Maintainability;

public class CyclomaticComplexityAnalyzer : BaseCodeAnalyzer
{
    public override string AnalyzerId => "MAINT001";
    public override string Name => "Cyclomatic Complexity Analyzer";
    public override IssueCategory Category => IssueCategory.Maintainability;

    private const int WarningThreshold = 10;
    private const int CriticalThreshold = 20;
    private const int BlockerThreshold = 30;

    public override Task<IEnumerable<AnalysisResult>> AnalyzeAsync(
        SyntaxTree syntaxTree,
        SemanticModel? semanticModel,
        string filePath)
    {
        var results = new List<AnalysisResult>();
        var root = syntaxTree.GetRoot();

        // Analyze methods
        var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();
        foreach (var method in methods)
        {
            var complexity = CalculateCyclomaticComplexity(method);
            var methodName = method.Identifier.Text;

            if (complexity >= BlockerThreshold)
            {
                results.Add(CreateResult(
                    "MAINT001",
                    "Extreme Cyclomatic Complexity",
                    $"Method '{methodName}' has cyclomatic complexity of {complexity}. Maximum recommended is {WarningThreshold}.",
                    filePath,
                    method.Identifier.GetLocation(),
                    Severity.Blocker,
                    $"{methodName}() - Complexity: {complexity}",
                    "Refactor this method into smaller, focused methods. Extract conditional logic into separate methods."));
            }
            else if (complexity >= CriticalThreshold)
            {
                results.Add(CreateResult(
                    "MAINT001",
                    "Very High Cyclomatic Complexity",
                    $"Method '{methodName}' has cyclomatic complexity of {complexity}. Maximum recommended is {WarningThreshold}.",
                    filePath,
                    method.Identifier.GetLocation(),
                    Severity.Critical,
                    $"{methodName}() - Complexity: {complexity}",
                    "Consider breaking this method into smaller, more focused methods."));
            }
            else if (complexity >= WarningThreshold)
            {
                results.Add(CreateResult(
                    "MAINT001",
                    "High Cyclomatic Complexity",
                    $"Method '{methodName}' has cyclomatic complexity of {complexity}. Maximum recommended is {WarningThreshold}.",
                    filePath,
                    method.Identifier.GetLocation(),
                    Severity.Major,
                    $"{methodName}() - Complexity: {complexity}",
                    "Consider refactoring to reduce complexity."));
            }
        }

        // Analyze properties with complex getters/setters
        var properties = root.DescendantNodes().OfType<PropertyDeclarationSyntax>();
        foreach (var property in properties)
        {
            var complexity = CalculatePropertyComplexity(property);
            if (complexity >= WarningThreshold)
            {
                results.Add(CreateResult(
                    "MAINT001",
                    "High Property Complexity",
                    $"Property '{property.Identifier.Text}' has complexity of {complexity}.",
                    filePath,
                    property.Identifier.GetLocation(),
                    Severity.Major,
                    $"{property.Identifier.Text} - Complexity: {complexity}",
                    "Consider extracting complex logic from property accessors into methods."));
            }
        }

        // Analyze lambdas and anonymous methods
        var lambdas = root.DescendantNodes().OfType<LambdaExpressionSyntax>();
        foreach (var lambda in lambdas)
        {
            var complexity = CalculateLambdaComplexity(lambda);
            if (complexity >= WarningThreshold)
            {
                results.Add(CreateResult(
                    "MAINT001",
                    "Complex Lambda Expression",
                    $"Lambda expression has complexity of {complexity}.",
                    filePath,
                    lambda.GetLocation(),
                    Severity.Major,
                    GetCodeSnippet(lambda),
                    "Consider extracting complex lambda logic into a named method."));
            }
        }

        return Task.FromResult<IEnumerable<AnalysisResult>>(results);
    }

    private static int CalculateCyclomaticComplexity(MethodDeclarationSyntax method)
    {
        // Start with 1 for the method itself
        int complexity = 1;

        if (method.Body != null)
        {
            complexity += CountComplexityNodes(method.Body);
        }
        else if (method.ExpressionBody != null)
        {
            complexity += CountComplexityNodes(method.ExpressionBody);
        }

        return complexity;
    }

    private static int CalculatePropertyComplexity(PropertyDeclarationSyntax property)
    {
        int complexity = 0;

        if (property.AccessorList != null)
        {
            foreach (var accessor in property.AccessorList.Accessors)
            {
                if (accessor.Body != null)
                {
                    complexity += CountComplexityNodes(accessor.Body);
                }
                else if (accessor.ExpressionBody != null)
                {
                    complexity += CountComplexityNodes(accessor.ExpressionBody);
                }
            }
        }
        else if (property.ExpressionBody != null)
        {
            complexity += CountComplexityNodes(property.ExpressionBody);
        }

        return complexity;
    }

    private static int CalculateLambdaComplexity(LambdaExpressionSyntax lambda)
    {
        int complexity = 1;

        if (lambda.Body != null)
        {
            complexity += CountComplexityNodes(lambda.Body);
        }

        return complexity;
    }

    private static int CountComplexityNodes(SyntaxNode node)
    {
        int count = 0;

        foreach (var descendant in node.DescendantNodes())
        {
            switch (descendant)
            {
                case IfStatementSyntax:
                case ConditionalExpressionSyntax:
                case CaseSwitchLabelSyntax:
                case CasePatternSwitchLabelSyntax:
                case WhileStatementSyntax:
                case ForStatementSyntax:
                case ForEachStatementSyntax:
                case DoStatementSyntax:
                case CatchClauseSyntax:
                case ConditionalAccessExpressionSyntax:
                    count++;
                    break;

                case BinaryExpressionSyntax binary:
                    // Count && and || operators
                    if (binary.IsKind(SyntaxKind.LogicalAndExpression) ||
                        binary.IsKind(SyntaxKind.LogicalOrExpression) ||
                        binary.IsKind(SyntaxKind.CoalesceExpression))
                    {
                        count++;
                    }
                    break;

                case SwitchExpressionSyntax switchExpr:
                    // Count switch expression arms
                    count += switchExpr.Arms.Count - 1; // -1 because default case doesn't add complexity
                    break;

                case GotoStatementSyntax:
                case BreakStatementSyntax when IsInSwitch(descendant):
                    // These don't add to cyclomatic complexity in typical counting
                    break;
            }
        }

        return count;
    }

    private static bool IsInSwitch(SyntaxNode node)
    {
        return node.Ancestors().Any(a => a is SwitchStatementSyntax);
    }
}
