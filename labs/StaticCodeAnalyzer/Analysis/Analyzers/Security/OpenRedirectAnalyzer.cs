using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StaticCodeAnalyzer.Analysis.Analyzers.Security;

public class OpenRedirectAnalyzer : BaseCodeAnalyzer
{
    public override string AnalyzerId => "SEC011";
    public override string Name => "Open Redirect Analyzer";
    public override IssueCategory Category => IssueCategory.Security;

    private static readonly HashSet<string> RedirectMethods = new()
    {
        "Redirect", "RedirectToAction", "RedirectToRoute", "RedirectPermanent",
        "RedirectToPage", "RedirectToActionPermanent", "LocalRedirect"
    };

    private static readonly HashSet<string> RedirectProperties = new()
    {
        "Location"
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
            var methodName = GetMethodName(invocation);
            var fullText = invocation.Expression.ToString();

            // Check for Redirect methods with user input
            if (RedirectMethods.Contains(methodName))
            {
                var args = invocation.ArgumentList.Arguments;
                if (args.Any())
                {
                    var urlArg = args.First().Expression;

                    // Skip LocalRedirect as it's meant to be safe
                    if (methodName == "LocalRedirect")
                    {
                        continue;
                    }

                    if (IsUserControlledUrl(urlArg))
                    {
                        results.Add(CreateResult(
                            "SEC011",
                            "Potential Open Redirect",
                            $"{methodName} called with user-controlled URL. This may allow open redirect attacks.",
                            filePath,
                            invocation.GetLocation(),
                            Severity.Major,
                            GetCodeSnippet(invocation),
                            "Validate redirect URLs against a whitelist or use LocalRedirect for relative URLs only.",
                            "CWE-601",
                            "A01:2021 - Broken Access Control"));
                    }
                }
            }

            // Check for Response.Redirect
            if (fullText.Contains("Response.Redirect"))
            {
                var args = invocation.ArgumentList.Arguments;
                if (args.Any() && IsUserControlledUrl(args.First().Expression))
                {
                    results.Add(CreateResult(
                        "SEC011",
                        "Potential Open Redirect via Response.Redirect",
                        "Response.Redirect called with potentially user-controlled URL.",
                        filePath,
                        invocation.GetLocation(),
                        Severity.Major,
                        GetCodeSnippet(invocation),
                        "Validate the redirect URL is a local URL or matches a whitelist.",
                        "CWE-601",
                        "A01:2021 - Broken Access Control"));
                }
            }
        }

        // Check for Location header assignments
        var assignments = root.DescendantNodes().OfType<AssignmentExpressionSyntax>();
        foreach (var assignment in assignments)
        {
            var leftText = assignment.Left.ToString();

            if (leftText.Contains("Location") &&
                (leftText.Contains("Headers") || leftText.Contains("Response")))
            {
                if (IsUserControlledUrl(assignment.Right))
                {
                    results.Add(CreateResult(
                        "SEC011",
                        "Potential Open Redirect via Location Header",
                        "Location header set with potentially user-controlled value.",
                        filePath,
                        assignment.GetLocation(),
                        Severity.Major,
                        GetCodeSnippet(assignment),
                        "Validate redirect URLs before setting Location header.",
                        "CWE-601",
                        "A01:2021 - Broken Access Control"));
                }
            }
        }

        // Check for return Redirect() in controllers
        var returnStatements = root.DescendantNodes().OfType<ReturnStatementSyntax>();
        foreach (var ret in returnStatements)
        {
            if (ret.Expression is InvocationExpressionSyntax retInvocation)
            {
                var methodName = GetMethodName(retInvocation);

                if (methodName == "Redirect" || methodName == "RedirectPermanent")
                {
                    var args = retInvocation.ArgumentList.Arguments;
                    if (args.Any() && IsUserControlledUrl(args.First().Expression))
                    {
                        results.Add(CreateResult(
                            "SEC011",
                            "Potential Open Redirect in Controller Return",
                            "Controller returning redirect with user-controlled URL.",
                            filePath,
                            ret.GetLocation(),
                            Severity.Major,
                            GetCodeSnippet(ret),
                            "Use Url.IsLocalUrl() to validate or use RedirectToLocal pattern.",
                            "CWE-601",
                            "A01:2021 - Broken Access Control"));
                    }
                }
            }
        }

        return Task.FromResult<IEnumerable<AnalysisResult>>(results);
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

    private static bool IsUserControlledUrl(ExpressionSyntax expression)
    {
        var text = expression.ToString().ToLowerInvariant();

        // Common parameter/input names
        var inputIndicators = new[]
        {
            "returnurl", "return_url", "redirect", "redirecturl", "redirect_url",
            "next", "url", "goto", "dest", "destination", "continue", "target",
            "request", "query", "param", "input"
        };

        if (inputIndicators.Any(ind => text.Contains(ind)))
            return true;

        // Check for direct parameter access
        if (text.Contains("request[") || text.Contains("query[") ||
            text.Contains("form[") || text.Contains("routedata["))
            return true;

        // Variable reference (conservative check)
        if (expression is IdentifierNameSyntax &&
            inputIndicators.Any(ind => text.Contains(ind)))
            return true;

        // String concatenation or interpolation building a URL
        if (expression is BinaryExpressionSyntax || expression is InterpolatedStringExpressionSyntax)
        {
            if (text.Contains("http") || text.Contains("://") || text.Contains("url"))
                return true;
        }

        return false;
    }
}
