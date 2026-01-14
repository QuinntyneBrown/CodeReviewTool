using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StaticCodeAnalyzer.Analysis.Analyzers.Security;

public class LdapInjectionAnalyzer : BaseCodeAnalyzer
{
    public override string AnalyzerId => "SEC010";
    public override string Name => "LDAP Injection Analyzer";
    public override IssueCategory Category => IssueCategory.Security;

    private static readonly HashSet<string> LdapTypes = new()
    {
        "DirectorySearcher", "DirectoryEntry", "LdapConnection",
        "SearchRequest", "SearchResponse"
    };

    private static readonly HashSet<string> LdapProperties = new()
    {
        "Filter", "Path", "SearchFilter", "LdapFilter"
    };

    public override Task<IEnumerable<AnalysisResult>> AnalyzeAsync(
        SyntaxTree syntaxTree,
        SemanticModel? semanticModel,
        string filePath)
    {
        var results = new List<AnalysisResult>();
        var root = syntaxTree.GetRoot();

        // Check for DirectorySearcher Filter property with dynamic input
        var assignments = root.DescendantNodes().OfType<AssignmentExpressionSyntax>();
        foreach (var assignment in assignments)
        {
            var leftText = assignment.Left.ToString();

            if (LdapProperties.Any(p => leftText.Contains(p)))
            {
                if (IsDynamicLdapInput(assignment.Right))
                {
                    results.Add(CreateResult(
                        "SEC010",
                        "Potential LDAP Injection",
                        "LDAP filter or path set with dynamic input. This may allow LDAP injection attacks.",
                        filePath,
                        assignment.GetLocation(),
                        Severity.Critical,
                        GetCodeSnippet(assignment),
                        "Sanitize LDAP filter input by escaping special characters: *, (, ), \\, NUL, /, and use parameterized LDAP queries where possible.",
                        "CWE-90",
                        "A03:2021 - Injection"));
                }
            }
        }

        // Check object initializers
        var objectCreations = root.DescendantNodes().OfType<ObjectCreationExpressionSyntax>();
        foreach (var creation in objectCreations)
        {
            var typeName = creation.Type.ToString();

            if (LdapTypes.Any(t => typeName.Contains(t)))
            {
                // Check constructor arguments
                var args = creation.ArgumentList?.Arguments;
                if (args != null)
                {
                    foreach (var arg in args)
                    {
                        if (IsDynamicLdapInput(arg.Expression))
                        {
                            results.Add(CreateResult(
                                "SEC010",
                                "Potential LDAP Injection in Constructor",
                                $"{typeName} created with potentially unsanitized input.",
                                filePath,
                                creation.GetLocation(),
                                Severity.Critical,
                                GetCodeSnippet(creation),
                                "Validate and escape LDAP special characters in user input.",
                                "CWE-90",
                                "A03:2021 - Injection"));
                            break;
                        }
                    }
                }

                // Check initializer expressions
                if (creation.Initializer != null)
                {
                    foreach (var expr in creation.Initializer.Expressions)
                    {
                        if (expr is AssignmentExpressionSyntax init)
                        {
                            var propName = init.Left.ToString();
                            if (LdapProperties.Any(p => propName.Contains(p)) &&
                                IsDynamicLdapInput(init.Right))
                            {
                                results.Add(CreateResult(
                                    "SEC010",
                                    "Potential LDAP Injection in Initializer",
                                    $"{propName} initialized with potentially unsanitized input.",
                                    filePath,
                                    init.GetLocation(),
                                    Severity.Critical,
                                    GetCodeSnippet(init),
                                    "Escape LDAP special characters before using in filters.",
                                    "CWE-90",
                                    "A03:2021 - Injection"));
                            }
                        }
                    }
                }
            }
        }

        // Check for string concatenation building LDAP filters
        var binaryExpressions = root.DescendantNodes().OfType<BinaryExpressionSyntax>()
            .Where(b => b.IsKind(SyntaxKind.AddExpression));

        foreach (var expr in binaryExpressions)
        {
            if (IsLdapFilterConstruction(expr))
            {
                results.Add(CreateResult(
                    "SEC010",
                    "LDAP Filter String Concatenation",
                    "LDAP filter appears to be built with string concatenation. This is vulnerable to injection.",
                    filePath,
                    expr.GetLocation(),
                    Severity.Critical,
                    GetCodeSnippet(expr),
                    "Use proper LDAP escaping or parameterized queries instead of string concatenation.",
                    "CWE-90",
                    "A03:2021 - Injection"));
            }
        }

        // Check for interpolated LDAP strings
        var interpolatedStrings = root.DescendantNodes().OfType<InterpolatedStringExpressionSyntax>();
        foreach (var interpolated in interpolatedStrings)
        {
            if (IsLdapFilterConstruction(interpolated))
            {
                results.Add(CreateResult(
                    "SEC010",
                    "LDAP Filter String Interpolation",
                    "LDAP filter built with string interpolation. Ensure proper escaping of values.",
                    filePath,
                    interpolated.GetLocation(),
                    Severity.Critical,
                    GetCodeSnippet(interpolated),
                    "Escape special LDAP characters in interpolated values.",
                    "CWE-90",
                    "A03:2021 - Injection"));
            }
        }

        return Task.FromResult<IEnumerable<AnalysisResult>>(results);
    }

    private static bool IsDynamicLdapInput(ExpressionSyntax expression)
    {
        // String concatenation
        if (expression is BinaryExpressionSyntax)
            return true;

        // String interpolation
        if (expression is InterpolatedStringExpressionSyntax)
            return true;

        // Variable that might be user input
        if (expression is IdentifierNameSyntax)
        {
            var name = expression.ToString().ToLowerInvariant();
            var inputIndicators = new[] { "input", "user", "name", "filter", "query", "search", "param", "request" };
            return inputIndicators.Any(ind => name.Contains(ind));
        }

        // String format call
        if (expression is InvocationExpressionSyntax inv)
        {
            var methodName = inv.Expression.ToString();
            return methodName.Contains("Format") || methodName.Contains("Concat");
        }

        return false;
    }

    private static bool IsLdapFilterConstruction(SyntaxNode node)
    {
        var text = node.ToString().ToLowerInvariant();

        // Common LDAP filter patterns
        var ldapPatterns = new[]
        {
            "(", ")", "=", "&", "|", "!", "cn=", "uid=", "samaccountname=",
            "objectclass=", "mail=", "ou=", "dc=", "distinguishedname="
        };

        return ldapPatterns.Count(p => text.Contains(p)) >= 2 &&
               node.DescendantNodes().OfType<IdentifierNameSyntax>().Any();
    }
}
