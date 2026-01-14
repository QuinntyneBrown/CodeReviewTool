using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StaticCodeAnalyzer.Analysis.Analyzers.Maintainability;

public class NestingDepthAnalyzer : BaseCodeAnalyzer
{
    public override string AnalyzerId => "MAINT005";
    public override string Name => "Nesting Depth Analyzer";
    public override IssueCategory Category => IssueCategory.Maintainability;

    private const int WarningThreshold = 4;
    private const int CriticalThreshold = 5;
    private const int BlockerThreshold = 6;

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
            var methodName = method.Identifier.Text;
            var deeplyNestedNodes = FindDeeplyNestedNodes(method);

            foreach (var (node, depth) in deeplyNestedNodes)
            {
                if (depth >= BlockerThreshold)
                {
                    results.Add(CreateResult(
                        "MAINT005",
                        "Extreme Nesting Depth",
                        $"Code in '{methodName}' is nested {depth} levels deep.",
                        filePath,
                        node.GetLocation(),
                        Severity.Blocker,
                        GetCodeSnippet(node),
                        "Refactor using early returns, guard clauses, or extract methods."));
                }
                else if (depth >= CriticalThreshold)
                {
                    results.Add(CreateResult(
                        "MAINT005",
                        "Very Deep Nesting",
                        $"Code in '{methodName}' is nested {depth} levels deep. Maximum recommended is {WarningThreshold}.",
                        filePath,
                        node.GetLocation(),
                        Severity.Critical,
                        GetCodeSnippet(node),
                        "Use early returns or extract nested logic into separate methods."));
                }
                else if (depth >= WarningThreshold)
                {
                    results.Add(CreateResult(
                        "MAINT005",
                        "Deep Nesting",
                        $"Code in '{methodName}' is nested {depth} levels deep. Maximum recommended is {WarningThreshold}.",
                        filePath,
                        node.GetLocation(),
                        Severity.Major,
                        GetCodeSnippet(node),
                        "Consider flattening with guard clauses or extracting methods."));
                }
            }
        }

        return Task.FromResult<IEnumerable<AnalysisResult>>(results);
    }

    private static List<(SyntaxNode Node, int Depth)> FindDeeplyNestedNodes(MethodDeclarationSyntax method)
    {
        var result = new List<(SyntaxNode, int)>();
        var maxDepthReported = new HashSet<int>(); // Track depths we've reported to avoid duplicates

        void Traverse(SyntaxNode node, int depth)
        {
            int newDepth = depth;

            if (IsNestingNode(node))
            {
                newDepth = depth + 1;

                // Only report the deepest occurrences to avoid noise
                if (newDepth >= WarningThreshold && !maxDepthReported.Contains(newDepth))
                {
                    result.Add((node, newDepth));
                    maxDepthReported.Add(newDepth);
                }
            }

            foreach (var child in node.ChildNodes())
            {
                Traverse(child, newDepth);
            }
        }

        if (method.Body != null)
        {
            foreach (var child in method.Body.ChildNodes())
            {
                Traverse(child, 0);
            }
        }

        return result;
    }

    private static bool IsNestingNode(SyntaxNode node)
    {
        return node is IfStatementSyntax ||
               node is ForStatementSyntax ||
               node is ForEachStatementSyntax ||
               node is WhileStatementSyntax ||
               node is DoStatementSyntax ||
               node is SwitchStatementSyntax ||
               node is TryStatementSyntax ||
               node is LockStatementSyntax ||
               node is UsingStatementSyntax ||
               (node is ElseClauseSyntax elseClause &&
                !(elseClause.Statement is IfStatementSyntax)); // else-if doesn't count as extra nesting
    }
}
