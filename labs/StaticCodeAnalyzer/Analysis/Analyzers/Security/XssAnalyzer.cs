using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StaticCodeAnalyzer.Analysis.Analyzers.Security;

public class XssAnalyzer : BaseCodeAnalyzer
{
    public override string AnalyzerId => "SEC003";
    public override string Name => "Cross-Site Scripting (XSS) Analyzer";
    public override IssueCategory Category => IssueCategory.Security;

    private static readonly HashSet<string> DangerousMethods = new()
    {
        "Write", "WriteLiteral", "WriteRaw", "WriteHtml", "RenderBody",
        "Html.Raw", "HtmlString"
    };

    private static readonly HashSet<string> ResponseWriteMethods = new()
    {
        "Response.Write", "HttpResponse.Write"
    };

    public override Task<IEnumerable<AnalysisResult>> AnalyzeAsync(
        SyntaxTree syntaxTree,
        SemanticModel? semanticModel,
        string filePath)
    {
        var results = new List<AnalysisResult>();
        var root = syntaxTree.GetRoot();

        var invocations = root.DescendantNodes().OfType<InvocationExpressionSyntax>();

        foreach (var invocation in invocations)
        {
            var methodName = GetFullMethodName(invocation);

            // Check for Html.Raw usage with dynamic content
            if (methodName.Contains("Html.Raw") || methodName.Contains("HtmlString"))
            {
                var args = invocation.ArgumentList.Arguments;
                if (args.Any(a => IsDynamicUserInput(a.Expression)))
                {
                    results.Add(CreateResult(
                        "SEC003",
                        "Potential XSS via Html.Raw",
                        "Html.Raw or HtmlString used with potentially untrusted input. This bypasses HTML encoding.",
                        filePath,
                        invocation.GetLocation(),
                        Severity.Critical,
                        GetCodeSnippet(invocation),
                        "Avoid using Html.Raw with user input. Use HTML encoding or a sanitization library.",
                        "CWE-79",
                        "A03:2021 - Injection"));
                }
            }

            // Check for Response.Write with dynamic content
            if (ResponseWriteMethods.Any(m => methodName.Contains(m)))
            {
                var args = invocation.ArgumentList.Arguments;
                if (args.Any(a => IsDynamicUserInput(a.Expression)))
                {
                    results.Add(CreateResult(
                        "SEC003",
                        "Potential XSS via Response.Write",
                        "Response.Write used with potentially untrusted input without encoding.",
                        filePath,
                        invocation.GetLocation(),
                        Severity.Critical,
                        GetCodeSnippet(invocation),
                        "Use HttpUtility.HtmlEncode or built-in encoding mechanisms.",
                        "CWE-79",
                        "A03:2021 - Injection"));
                }
            }

            // Check for JavaScript injection patterns
            if (IsJavaScriptInjectionPattern(invocation))
            {
                results.Add(CreateResult(
                    "SEC003",
                    "Potential JavaScript Injection",
                    "Dynamic content detected in JavaScript context. This may lead to XSS attacks.",
                    filePath,
                    invocation.GetLocation(),
                    Severity.Critical,
                    GetCodeSnippet(invocation),
                    "Use JavaScript encoding for dynamic values in JavaScript contexts.",
                    "CWE-79",
                    "A03:2021 - Injection"));
            }
        }

        // Check for string building with HTML content
        var binaryExpressions = root.DescendantNodes().OfType<BinaryExpressionSyntax>()
            .Where(b => b.IsKind(SyntaxKind.AddExpression));

        foreach (var expr in binaryExpressions)
        {
            if (ContainsHtmlTags(expr) && ContainsVariableReference(expr))
            {
                results.Add(CreateResult(
                    "SEC003",
                    "Potential XSS via String Concatenation",
                    "HTML string built with dynamic content. Ensure proper encoding.",
                    filePath,
                    expr.GetLocation(),
                    Severity.Major,
                    GetCodeSnippet(expr),
                    "Use a templating engine with automatic encoding or encode values manually.",
                    "CWE-79",
                    "A03:2021 - Injection"));
            }
        }

        return Task.FromResult<IEnumerable<AnalysisResult>>(results);
    }

    private static string GetFullMethodName(InvocationExpressionSyntax invocation)
    {
        return invocation.Expression.ToString();
    }

    private static bool IsDynamicUserInput(ExpressionSyntax expression)
    {
        var text = expression.ToString().ToLowerInvariant();
        var inputIndicators = new[] { "request", "input", "query", "form", "param", "user", "data", "model" };
        return inputIndicators.Any(ind => text.Contains(ind)) ||
               expression is BinaryExpressionSyntax ||
               expression is InterpolatedStringExpressionSyntax ||
               expression is IdentifierNameSyntax;
    }

    private static bool IsJavaScriptInjectionPattern(InvocationExpressionSyntax invocation)
    {
        var text = invocation.ToString().ToLowerInvariant();
        return (text.Contains("<script") || text.Contains("javascript:") ||
                text.Contains("onclick") || text.Contains("onerror") ||
                text.Contains("onload")) &&
               invocation.ArgumentList.Arguments.Any(a => IsDynamicUserInput(a.Expression));
    }

    private static bool ContainsHtmlTags(BinaryExpressionSyntax expr)
    {
        var text = expr.ToString();
        return text.Contains("<") && text.Contains(">") ||
               text.Contains("&lt;") || text.Contains("&gt;");
    }

    private static bool ContainsVariableReference(BinaryExpressionSyntax expr)
    {
        return expr.DescendantNodes().OfType<IdentifierNameSyntax>().Any();
    }
}
