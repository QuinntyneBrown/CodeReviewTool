using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StaticCodeAnalyzer.Analysis.Analyzers.Security;

public class SensitiveDataExposureAnalyzer : BaseCodeAnalyzer
{
    public override string AnalyzerId => "SEC012";
    public override string Name => "Sensitive Data Exposure Analyzer";
    public override IssueCategory Category => IssueCategory.Security;

    private static readonly HashSet<string> SensitiveDataIndicators = new(StringComparer.OrdinalIgnoreCase)
    {
        "password", "passwd", "pwd", "secret", "creditcard", "credit_card",
        "ssn", "socialsecurity", "social_security", "dob", "dateofbirth",
        "bankaccount", "bank_account", "routingnumber", "routing_number",
        "pin", "cvv", "cvc", "securitycode", "security_code"
    };

    private static readonly HashSet<string> LoggingMethods = new()
    {
        "Log", "LogInformation", "LogWarning", "LogError", "LogDebug", "LogTrace", "LogCritical",
        "WriteLine", "Write", "Info", "Warn", "Error", "Debug", "Trace", "Fatal",
        "Console.WriteLine", "Console.Write", "Trace.WriteLine", "Debug.WriteLine"
    };

    public override Task<IEnumerable<AnalysisResult>> AnalyzeAsync(
        SyntaxTree syntaxTree,
        SemanticModel? semanticModel,
        string filePath)
    {
        var results = new List<AnalysisResult>();
        var root = syntaxTree.GetRoot();

        // Check for sensitive data in logging
        var invocations = root.DescendantNodes().OfType<InvocationExpressionSyntax>();
        foreach (var invocation in invocations)
        {
            var methodName = GetMethodName(invocation);
            var fullText = invocation.Expression.ToString();

            if (IsLoggingMethod(methodName) || IsLoggingMethod(fullText))
            {
                var args = invocation.ArgumentList.Arguments;
                foreach (var arg in args)
                {
                    if (ContainsSensitiveData(arg.Expression))
                    {
                        results.Add(CreateResult(
                            "SEC012",
                            "Sensitive Data in Logging",
                            "Potentially sensitive data being logged. This may expose secrets in log files.",
                            filePath,
                            invocation.GetLocation(),
                            Severity.Critical,
                            GetCodeSnippet(invocation),
                            "Avoid logging sensitive data. Use masking or redaction for necessary logging.",
                            "CWE-532",
                            "A09:2021 - Security Logging and Monitoring Failures"));
                        break;
                    }
                }
            }
        }

        // Check for sensitive data in exception messages
        var throwStatements = root.DescendantNodes().OfType<ThrowStatementSyntax>();
        foreach (var throwStmt in throwStatements)
        {
            if (throwStmt.Expression is ObjectCreationExpressionSyntax creation)
            {
                var args = creation.ArgumentList?.Arguments;
                if (args != null)
                {
                    foreach (var arg in args)
                    {
                        if (ContainsSensitiveData(arg.Expression))
                        {
                            results.Add(CreateResult(
                                "SEC012",
                                "Sensitive Data in Exception",
                                "Potentially sensitive data included in exception message.",
                                filePath,
                                throwStmt.GetLocation(),
                                Severity.Major,
                                GetCodeSnippet(throwStmt),
                                "Do not include sensitive data in exception messages. Use generic messages.",
                                "CWE-209",
                                "A09:2021 - Security Logging and Monitoring Failures"));
                            break;
                        }
                    }
                }
            }
        }

        // Check for sensitive data returned in HTTP responses
        foreach (var invocation in invocations)
        {
            var fullText = invocation.Expression.ToString();

            if (fullText.Contains("Ok(") || fullText.Contains("Json(") ||
                fullText.Contains("Content(") || fullText.Contains("StatusCode("))
            {
                var args = invocation.ArgumentList.Arguments;
                foreach (var arg in args)
                {
                    if (ContainsSensitiveData(arg.Expression))
                    {
                        results.Add(CreateResult(
                            "SEC012",
                            "Sensitive Data in HTTP Response",
                            "Potentially sensitive data may be exposed in HTTP response.",
                            filePath,
                            invocation.GetLocation(),
                            Severity.Major,
                            GetCodeSnippet(invocation),
                            "Ensure sensitive data is not included in API responses. Use DTOs to control exposed fields.",
                            "CWE-200",
                            "A01:2021 - Broken Access Control"));
                        break;
                    }
                }
            }
        }

        // Check for sensitive data in query strings
        foreach (var invocation in invocations)
        {
            var text = invocation.ToString();

            if ((text.Contains("QueryString") || text.Contains("query=") ||
                 text.Contains("?") && text.Contains("=")) &&
                SensitiveDataIndicators.Any(s => text.ToLowerInvariant().Contains(s.ToLowerInvariant())))
            {
                results.Add(CreateResult(
                    "SEC012",
                    "Sensitive Data in Query String",
                    "Sensitive data may be included in URL query string. This exposes data in logs and browser history.",
                    filePath,
                    invocation.GetLocation(),
                    Severity.Major,
                    GetCodeSnippet(invocation),
                    "Use POST requests with body for sensitive data. Never include secrets in URLs.",
                    "CWE-598",
                    "A04:2021 - Insecure Design"));
            }
        }

        // Check for ToString() on sensitive types
        foreach (var invocation in invocations)
        {
            var fullText = invocation.ToString().ToLowerInvariant();

            if (fullText.Contains(".tostring()") &&
                SensitiveDataIndicators.Any(s => fullText.Contains(s.ToLowerInvariant())))
            {
                // Check context - is it being logged or displayed?
                var parent = invocation.Parent;
                if (parent is ArgumentSyntax arg)
                {
                    var parentInvocation = arg.Ancestors().OfType<InvocationExpressionSyntax>().FirstOrDefault();
                    if (parentInvocation != null && IsLoggingMethod(GetMethodName(parentInvocation)))
                    {
                        results.Add(CreateResult(
                            "SEC012",
                            "Sensitive Data ToString in Logging",
                            "Sensitive object converted to string for potential logging or display.",
                            filePath,
                            invocation.GetLocation(),
                            Severity.Major,
                            GetCodeSnippet(invocation),
                            "Avoid converting sensitive data to strings for logging. Use masked representations.",
                            "CWE-532",
                            "A09:2021 - Security Logging and Monitoring Failures"));
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

    private static bool IsLoggingMethod(string methodName)
    {
        return LoggingMethods.Any(m =>
            methodName.Equals(m, StringComparison.OrdinalIgnoreCase) ||
            methodName.Contains(m, StringComparison.OrdinalIgnoreCase));
    }

    private static bool ContainsSensitiveData(ExpressionSyntax expression)
    {
        var text = expression.ToString().ToLowerInvariant();
        return SensitiveDataIndicators.Any(s => text.Contains(s.ToLowerInvariant()));
    }
}
