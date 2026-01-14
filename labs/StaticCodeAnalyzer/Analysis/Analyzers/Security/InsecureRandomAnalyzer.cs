using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StaticCodeAnalyzer.Analysis.Analyzers.Security;

public class InsecureRandomAnalyzer : BaseCodeAnalyzer
{
    public override string AnalyzerId => "SEC009";
    public override string Name => "Insecure Random Number Generator Analyzer";
    public override IssueCategory Category => IssueCategory.Security;

    private static readonly HashSet<string> SecurityContextIndicators = new(StringComparer.OrdinalIgnoreCase)
    {
        "token", "key", "secret", "password", "salt", "nonce", "iv",
        "session", "auth", "crypto", "secure", "random", "guid", "id",
        "otp", "code", "pin", "verification", "reset"
    };

    public override Task<IEnumerable<AnalysisResult>> AnalyzeAsync(
        SyntaxTree syntaxTree,
        SemanticModel? semanticModel,
        string filePath)
    {
        var results = new List<AnalysisResult>();
        var root = syntaxTree.GetRoot();

        // Find Random class instantiations
        var objectCreations = root.DescendantNodes().OfType<ObjectCreationExpressionSyntax>();
        foreach (var creation in objectCreations)
        {
            var typeName = creation.Type.ToString();

            if (typeName == "Random" || typeName == "System.Random")
            {
                // Check if used in security context
                var context = GetSecurityContext(creation);
                if (context != null)
                {
                    results.Add(CreateResult(
                        "SEC009",
                        "Insecure Random in Security Context",
                        $"System.Random used in apparent security context ({context}). This is not cryptographically secure.",
                        filePath,
                        creation.GetLocation(),
                        Severity.Critical,
                        GetCodeSnippet(creation),
                        "Use RandomNumberGenerator or RNGCryptoServiceProvider for security-sensitive random values.",
                        "CWE-330",
                        "A02:2021 - Cryptographic Failures"));
                }
                else
                {
                    // Still warn but with lower severity
                    results.Add(CreateResult(
                        "SEC009",
                        "Potentially Insecure Random Usage",
                        "System.Random is not cryptographically secure. Ensure it's not used for security purposes.",
                        filePath,
                        creation.GetLocation(),
                        Severity.Minor,
                        GetCodeSnippet(creation),
                        "For security purposes, use RandomNumberGenerator instead.",
                        "CWE-330",
                        "A02:2021 - Cryptographic Failures"));
                }
            }
        }

        // Check for Random.Next(), Random.NextBytes() in security contexts
        var invocations = root.DescendantNodes().OfType<InvocationExpressionSyntax>();
        foreach (var invocation in invocations)
        {
            var methodText = invocation.Expression.ToString();

            if (methodText.Contains("Random") &&
                (methodText.Contains("Next") || methodText.Contains("NextBytes") ||
                 methodText.Contains("NextDouble")))
            {
                var context = GetSecurityContext(invocation);
                if (context != null)
                {
                    results.Add(CreateResult(
                        "SEC009",
                        "Insecure Random Method in Security Context",
                        $"Random number generation method used in security context ({context}).",
                        filePath,
                        invocation.GetLocation(),
                        Severity.Critical,
                        GetCodeSnippet(invocation),
                        "Use RandomNumberGenerator.GetBytes() or similar cryptographic methods.",
                        "CWE-330",
                        "A02:2021 - Cryptographic Failures"));
                }
            }
        }

        // Check for Guid.NewGuid() used as security token
        foreach (var invocation in invocations)
        {
            var methodText = invocation.Expression.ToString();

            if (methodText.Contains("Guid.NewGuid"))
            {
                var context = GetSecurityContext(invocation);
                if (context != null &&
                    (context.Contains("token") || context.Contains("secret") ||
                     context.Contains("key") || context.Contains("auth")))
                {
                    results.Add(CreateResult(
                        "SEC009",
                        "GUID Used as Security Token",
                        "Guid.NewGuid() used as apparent security token. GUIDs are not cryptographically random.",
                        filePath,
                        invocation.GetLocation(),
                        Severity.Major,
                        GetCodeSnippet(invocation),
                        "Use cryptographically secure random bytes for security tokens.",
                        "CWE-330",
                        "A02:2021 - Cryptographic Failures"));
                }
            }
        }

        // Check for predictable seed values
        foreach (var creation in objectCreations)
        {
            var typeName = creation.Type.ToString();

            if (typeName == "Random" || typeName == "System.Random")
            {
                var args = creation.ArgumentList?.Arguments;
                if (args != null && args.Value.Any())
                {
                    var seedArg = args.Value.First().Expression;

                    // Check for constant seed
                    if (seedArg is LiteralExpressionSyntax)
                    {
                        results.Add(CreateResult(
                            "SEC009",
                            "Predictable Random Seed",
                            "Random initialized with constant seed, making output predictable.",
                            filePath,
                            creation.GetLocation(),
                            Severity.Major,
                            GetCodeSnippet(creation),
                            "Avoid fixed seeds for Random. For security, use RandomNumberGenerator.",
                            "CWE-330",
                            "A02:2021 - Cryptographic Failures"));
                    }

                    // Check for Environment.TickCount as seed
                    if (seedArg.ToString().Contains("TickCount") ||
                        seedArg.ToString().Contains("DateTime"))
                    {
                        results.Add(CreateResult(
                            "SEC009",
                            "Weak Random Seed",
                            "Random seeded with predictable value (time-based). Output may be guessable.",
                            filePath,
                            creation.GetLocation(),
                            Severity.Major,
                            GetCodeSnippet(creation),
                            "For security purposes, use RandomNumberGenerator which doesn't need seeding.",
                            "CWE-330",
                            "A02:2021 - Cryptographic Failures"));
                    }
                }
            }
        }

        return Task.FromResult<IEnumerable<AnalysisResult>>(results);
    }

    private static string? GetSecurityContext(SyntaxNode node)
    {
        // Check variable name
        var variableDeclaration = node.Ancestors().OfType<VariableDeclaratorSyntax>().FirstOrDefault();
        if (variableDeclaration != null)
        {
            var varName = variableDeclaration.Identifier.Text.ToLowerInvariant();
            var match = SecurityContextIndicators.FirstOrDefault(s => varName.Contains(s.ToLowerInvariant()));
            if (match != null) return match;
        }

        // Check assignment target
        var assignment = node.Ancestors().OfType<AssignmentExpressionSyntax>().FirstOrDefault();
        if (assignment != null)
        {
            var leftName = assignment.Left.ToString().ToLowerInvariant();
            var match = SecurityContextIndicators.FirstOrDefault(s => leftName.Contains(s.ToLowerInvariant()));
            if (match != null) return match;
        }

        // Check method name
        var method = node.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
        if (method != null)
        {
            var methodName = method.Identifier.Text.ToLowerInvariant();
            var match = SecurityContextIndicators.FirstOrDefault(s => methodName.Contains(s.ToLowerInvariant()));
            if (match != null) return match;
        }

        // Check class name
        var classDecl = node.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
        if (classDecl != null)
        {
            var className = classDecl.Identifier.Text.ToLowerInvariant();
            var match = SecurityContextIndicators.FirstOrDefault(s => className.Contains(s.ToLowerInvariant()));
            if (match != null) return match;
        }

        return null;
    }
}
