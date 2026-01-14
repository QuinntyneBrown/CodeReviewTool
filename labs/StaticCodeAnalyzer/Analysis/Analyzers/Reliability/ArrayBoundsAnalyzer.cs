using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StaticCodeAnalyzer.Analysis.Analyzers.Reliability;

public class ArrayBoundsAnalyzer : BaseCodeAnalyzer
{
    public override string AnalyzerId => "REL007";
    public override string Name => "Array Bounds Analyzer";
    public override IssueCategory Category => IssueCategory.Reliability;

    public override Task<IEnumerable<AnalysisResult>> AnalyzeAsync(
        SyntaxTree syntaxTree,
        SemanticModel? semanticModel,
        string filePath)
    {
        var results = new List<AnalysisResult>();
        var root = syntaxTree.GetRoot();

        // Check for array access without bounds checking
        var elementAccesses = root.DescendantNodes().OfType<ElementAccessExpressionSyntax>();

        foreach (var access in elementAccesses)
        {
            var containingMethod = access.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
            if (containingMethod == null)
                continue;

            // Check for hardcoded negative index
            foreach (var arg in access.ArgumentList.Arguments)
            {
                if (arg.Expression is PrefixUnaryExpressionSyntax prefix &&
                    prefix.IsKind(SyntaxKind.UnaryMinusExpression))
                {
                    results.Add(CreateResult(
                        "REL007",
                        "Negative Array Index",
                        "Negative index used for array access.",
                        filePath,
                        access.GetLocation(),
                        Severity.Critical,
                        GetCodeSnippet(access),
                        "Use ^1 syntax for index from end, or ensure index is non-negative.",
                        "CWE-129"));
                }

                // Check for index that might be from user input
                if (IsUserInputIndex(arg.Expression))
                {
                    // Check if there's a bounds check
                    if (!HasBoundsCheck(access, containingMethod))
                    {
                        results.Add(CreateResult(
                            "REL007",
                            "Unchecked Array Index from Input",
                            "Array index may come from user input without visible bounds checking.",
                            filePath,
                            access.GetLocation(),
                            Severity.Major,
                            GetCodeSnippet(access),
                            "Validate array indices before use. Check that index >= 0 and index < array.Length.",
                            "CWE-129"));
                    }
                }
            }
        }

        // Check for off-by-one in loops accessing arrays
        var forStatements = root.DescendantNodes().OfType<ForStatementSyntax>();
        foreach (var forStmt in forStatements)
        {
            if (forStmt.Condition is BinaryExpressionSyntax condition)
            {
                var conditionText = condition.ToString();

                // Check for <= instead of <
                if (condition.IsKind(SyntaxKind.LessThanOrEqualExpression))
                {
                    var rightText = condition.Right.ToString();
                    if (rightText.Contains("Length") || rightText.Contains("Count"))
                    {
                        results.Add(CreateResult(
                            "REL007",
                            "Potential Off-By-One Error",
                            "Loop condition uses '<=' with Length/Count. This may cause IndexOutOfRangeException.",
                            filePath,
                            forStmt.GetLocation(),
                            Severity.Critical,
                            GetCodeSnippet(forStmt),
                            "Use '<' instead of '<=' when comparing to Length or Count.",
                            "CWE-193"));
                    }
                }
            }
        }

        // Check for array creation with size from unchecked input
        var arrayCreations = root.DescendantNodes().OfType<ArrayCreationExpressionSyntax>();
        foreach (var creation in arrayCreations)
        {
            if (creation.Type.RankSpecifiers.Count > 0)
            {
                var rankSpecifier = creation.Type.RankSpecifiers[0];
                foreach (var size in rankSpecifier.Sizes)
                {
                    if (size is IdentifierNameSyntax identifier)
                    {
                        var varName = identifier.Identifier.Text.ToLowerInvariant();
                        var inputIndicators = new[] { "input", "param", "size", "length", "count", "request" };

                        if (inputIndicators.Any(i => varName.Contains(i)))
                        {
                            results.Add(CreateResult(
                                "REL007",
                                "Array Size from Untrusted Input",
                                "Array created with size from potentially untrusted input. This could cause OutOfMemoryException.",
                                filePath,
                                creation.GetLocation(),
                                Severity.Major,
                                GetCodeSnippet(creation),
                                "Validate and limit array size before creation. Set reasonable maximum bounds.",
                                "CWE-789"));
                        }
                    }
                }
            }
        }

        // Check for string indexing without length check
        var stringAccesses = root.DescendantNodes().OfType<ElementAccessExpressionSyntax>()
            .Where(e => IsStringAccess(e));

        foreach (var access in stringAccesses)
        {
            var containingMethod = access.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
            if (containingMethod != null && !HasBoundsCheck(access, containingMethod))
            {
                // Check if it's accessing a variable index
                var arg = access.ArgumentList.Arguments.FirstOrDefault();
                if (arg?.Expression is IdentifierNameSyntax)
                {
                    results.Add(CreateResult(
                        "REL007",
                        "String Index Without Bounds Check",
                        "String character access without visible length check.",
                        filePath,
                        access.GetLocation(),
                        Severity.Minor,
                        GetCodeSnippet(access),
                        "Check string length before accessing characters by index.",
                        "CWE-129"));
                }
            }
        }

        // Check for LINQ ElementAt without bounds check
        var invocations = root.DescendantNodes().OfType<InvocationExpressionSyntax>();
        foreach (var invocation in invocations)
        {
            var methodName = GetMethodName(invocation);
            if (methodName == "ElementAt")
            {
                var containingMethod = invocation.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
                if (containingMethod != null)
                {
                    var args = invocation.ArgumentList.Arguments;
                    if (args.Any() && args.First().Expression is IdentifierNameSyntax)
                    {
                        results.Add(CreateResult(
                            "REL007",
                            "ElementAt Without Bounds Check",
                            "LINQ ElementAt() called with variable index. Consider using ElementAtOrDefault().",
                            filePath,
                            invocation.GetLocation(),
                            Severity.Minor,
                            GetCodeSnippet(invocation),
                            "Use ElementAtOrDefault() to safely handle out-of-range indices.",
                            "CWE-129"));
                    }
                }
            }
        }

        return Task.FromResult<IEnumerable<AnalysisResult>>(results);
    }

    private static bool IsUserInputIndex(ExpressionSyntax expression)
    {
        var text = expression.ToString().ToLowerInvariant();
        var inputIndicators = new[] { "input", "index", "i", "param", "request", "query" };
        return inputIndicators.Any(ind => text.Contains(ind)) ||
               expression is IdentifierNameSyntax;
    }

    private static bool HasBoundsCheck(ElementAccessExpressionSyntax access, MethodDeclarationSyntax method)
    {
        var arrayExpr = access.Expression.ToString();

        // Look for if statements before this access that check bounds
        var precedingNodes = method.DescendantNodes()
            .TakeWhile(n => n != access);

        foreach (var node in precedingNodes)
        {
            if (node is IfStatementSyntax ifStmt)
            {
                var conditionText = ifStmt.Condition.ToString();

                // Check for Length/Count comparison
                if ((conditionText.Contains("Length") || conditionText.Contains("Count")) &&
                    (conditionText.Contains("<") || conditionText.Contains(">") ||
                     conditionText.Contains(">=") || conditionText.Contains("<=")))
                {
                    return true;
                }

                // Check for null/empty check
                if (conditionText.Contains("null") ||
                    conditionText.Contains("IsNullOrEmpty") ||
                    conditionText.Contains("Any()"))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsStringAccess(ElementAccessExpressionSyntax access)
    {
        var exprText = access.Expression.ToString().ToLowerInvariant();
        return exprText.Contains("string") || exprText.EndsWith("str") ||
               exprText.EndsWith("text") || exprText.EndsWith("name");
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
