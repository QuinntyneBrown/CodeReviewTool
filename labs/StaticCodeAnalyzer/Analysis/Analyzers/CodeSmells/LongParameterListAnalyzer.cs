using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StaticCodeAnalyzer.Analysis.Analyzers.CodeSmells;

public class LongParameterListAnalyzer : BaseCodeAnalyzer
{
    public override string AnalyzerId => "SMELL005";
    public override string Name => "Long Parameter List Analyzer";
    public override IssueCategory Category => IssueCategory.CodeSmell;

    public override Task<IEnumerable<AnalysisResult>> AnalyzeAsync(
        SyntaxTree syntaxTree,
        SemanticModel? semanticModel,
        string filePath)
    {
        var results = new List<AnalysisResult>();
        var root = syntaxTree.GetRoot();

        // Check for out/ref parameter abuse
        var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();

        foreach (var method in methods)
        {
            var outRefParams = method.ParameterList.Parameters
                .Where(p => p.Modifiers.Any(m =>
                    m.IsKind(SyntaxKind.OutKeyword) ||
                    m.IsKind(SyntaxKind.RefKeyword)))
                .ToList();

            if (outRefParams.Count > 2)
            {
                results.Add(CreateResult(
                    "SMELL005",
                    "Too Many Out/Ref Parameters",
                    $"Method '{method.Identifier.Text}' has {outRefParams.Count} out/ref parameters.",
                    filePath,
                    method.Identifier.GetLocation(),
                    Severity.Major,
                    $"{method.Identifier.Text}({outRefParams.Count} out/ref params)",
                    "Consider returning a tuple or a custom result object instead."));
            }

            // Check for params array abuse
            var paramsParam = method.ParameterList.Parameters
                .FirstOrDefault(p => p.Modifiers.Any(m => m.IsKind(SyntaxKind.ParamsKeyword)));

            if (paramsParam != null && method.ParameterList.Parameters.Count > 3)
            {
                results.Add(CreateResult(
                    "SMELL005",
                    "Many Parameters with Params Array",
                    $"Method '{method.Identifier.Text}' has many parameters plus a params array.",
                    filePath,
                    method.Identifier.GetLocation(),
                    Severity.Minor,
                    $"{method.Identifier.Text}",
                    "Consider using an options object pattern instead."));
            }

            // Check for nullable parameter chains
            var nullableParams = method.ParameterList.Parameters
                .Where(p =>
                    p.Type?.ToString().EndsWith("?") == true ||
                    p.Default != null)
                .ToList();

            if (nullableParams.Count >= 4)
            {
                results.Add(CreateResult(
                    "SMELL005",
                    "Too Many Optional Parameters",
                    $"Method '{method.Identifier.Text}' has {nullableParams.Count} optional/nullable parameters.",
                    filePath,
                    method.Identifier.GetLocation(),
                    Severity.Minor,
                    $"{method.Identifier.Text}({nullableParams.Count} optional params)",
                    "Consider using the Builder pattern or an options class."));
            }

            // Check for poor parameter naming
            foreach (var param in method.ParameterList.Parameters)
            {
                var paramName = param.Identifier.Text;

                // Check for single character names (except common ones like 'x', 'y', 'i', 'j')
                if (paramName.Length == 1 &&
                    !new[] { "x", "y", "z", "i", "j", "k", "n", "t" }.Contains(paramName.ToLower()))
                {
                    results.Add(CreateResult(
                        "SMELL005",
                        "Poor Parameter Name",
                        $"Parameter '{paramName}' in method '{method.Identifier.Text}' has a non-descriptive name.",
                        filePath,
                        param.GetLocation(),
                        Severity.Minor,
                        $"{method.Identifier.Text}({paramName})",
                        "Use descriptive parameter names that indicate the parameter's purpose."));
                }

                // Check for type-based names
                var typeName = param.Type?.ToString().ToLower() ?? "";
                if (paramName.ToLower() == typeName ||
                    paramName.ToLower() == typeName.TrimEnd('?'))
                {
                    results.Add(CreateResult(
                        "SMELL005",
                        "Type-Based Parameter Name",
                        $"Parameter '{paramName}' is named after its type. Use a more descriptive name.",
                        filePath,
                        param.GetLocation(),
                        Severity.Info,
                        $"{typeName} {paramName}",
                        "Name parameters based on their role, not their type."));
                }
            }
        }

        return Task.FromResult<IEnumerable<AnalysisResult>>(results);
    }
}
