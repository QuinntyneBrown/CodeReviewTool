using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StaticCodeAnalyzer.Analysis.Analyzers.Maintainability;

public class ParameterCountAnalyzer : BaseCodeAnalyzer
{
    public override string AnalyzerId => "MAINT004";
    public override string Name => "Parameter Count Analyzer";
    public override IssueCategory Category => IssueCategory.Maintainability;

    private const int WarningThreshold = 5;
    private const int CriticalThreshold = 7;
    private const int BlockerThreshold = 10;

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
            var paramCount = method.ParameterList.Parameters.Count;
            var methodName = method.Identifier.Text;

            if (paramCount >= BlockerThreshold)
            {
                results.Add(CreateResult(
                    "MAINT004",
                    "Extreme Parameter Count",
                    $"Method '{methodName}' has {paramCount} parameters. Maximum recommended is {WarningThreshold}.",
                    filePath,
                    method.Identifier.GetLocation(),
                    Severity.Blocker,
                    $"{methodName}({paramCount} params)",
                    "Consider using a parameter object, builder pattern, or breaking up the method."));
            }
            else if (paramCount >= CriticalThreshold)
            {
                results.Add(CreateResult(
                    "MAINT004",
                    "Too Many Parameters",
                    $"Method '{methodName}' has {paramCount} parameters. Maximum recommended is {WarningThreshold}.",
                    filePath,
                    method.Identifier.GetLocation(),
                    Severity.Critical,
                    $"{methodName}({paramCount} params)",
                    "Group related parameters into a class or struct."));
            }
            else if (paramCount >= WarningThreshold)
            {
                results.Add(CreateResult(
                    "MAINT004",
                    "Many Parameters",
                    $"Method '{methodName}' has {paramCount} parameters. Maximum recommended is {WarningThreshold}.",
                    filePath,
                    method.Identifier.GetLocation(),
                    Severity.Major,
                    $"{methodName}({paramCount} params)",
                    "Consider introducing a parameter object to group related parameters."));
            }

            // Check for boolean parameters (flag arguments)
            var boolParams = method.ParameterList.Parameters
                .Where(p => p.Type?.ToString() == "bool" || p.Type?.ToString() == "Boolean");

            if (boolParams.Count() > 1)
            {
                results.Add(CreateResult(
                    "MAINT004",
                    "Multiple Boolean Parameters",
                    $"Method '{methodName}' has multiple boolean parameters. This can be confusing at call sites.",
                    filePath,
                    method.Identifier.GetLocation(),
                    Severity.Minor,
                    $"{methodName} - {boolParams.Count()} bool params",
                    "Consider using enums or separate methods instead of boolean flags."));
            }

            // Check for consecutive parameters of the same type
            var parameters = method.ParameterList.Parameters.ToList();
            for (int i = 0; i < parameters.Count - 2; i++)
            {
                var type1 = parameters[i].Type?.ToString();
                var type2 = parameters[i + 1].Type?.ToString();
                var type3 = parameters[i + 2].Type?.ToString();

                if (type1 == type2 && type2 == type3 && type1 == "string")
                {
                    results.Add(CreateResult(
                        "MAINT004",
                        "Consecutive String Parameters",
                        $"Method '{methodName}' has 3+ consecutive string parameters. Easy to mix up at call sites.",
                        filePath,
                        parameters[i].GetLocation(),
                        Severity.Minor,
                        $"{methodName}(..., string, string, string, ...)",
                        "Consider using a record or class to group related strings with named properties."));
                    break;
                }
            }
        }

        // Check constructors
        var constructors = root.DescendantNodes().OfType<ConstructorDeclarationSyntax>();
        foreach (var ctor in constructors)
        {
            var paramCount = ctor.ParameterList.Parameters.Count;
            var className = ctor.Identifier.Text;

            if (paramCount >= CriticalThreshold)
            {
                results.Add(CreateResult(
                    "MAINT004",
                    "Constructor With Too Many Parameters",
                    $"Constructor for '{className}' has {paramCount} parameters.",
                    filePath,
                    ctor.Identifier.GetLocation(),
                    Severity.Major,
                    $"{className}({paramCount} params)",
                    "Consider using the Builder pattern or breaking up dependencies."));
            }
        }

        // Check delegates and lambdas
        var lambdas = root.DescendantNodes().OfType<ParenthesizedLambdaExpressionSyntax>();
        foreach (var lambda in lambdas)
        {
            var paramCount = lambda.ParameterList.Parameters.Count;
            if (paramCount >= WarningThreshold)
            {
                results.Add(CreateResult(
                    "MAINT004",
                    "Lambda With Many Parameters",
                    $"Lambda expression has {paramCount} parameters.",
                    filePath,
                    lambda.GetLocation(),
                    Severity.Minor,
                    GetCodeSnippet(lambda),
                    "Consider extracting to a named method or using a parameter object."));
            }
        }

        return Task.FromResult<IEnumerable<AnalysisResult>>(results);
    }
}
