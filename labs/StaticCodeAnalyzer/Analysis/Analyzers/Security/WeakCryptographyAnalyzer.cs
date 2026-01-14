using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StaticCodeAnalyzer.Analysis.Analyzers.Security;

public class WeakCryptographyAnalyzer : BaseCodeAnalyzer
{
    public override string AnalyzerId => "SEC008";
    public override string Name => "Weak Cryptography Analyzer";
    public override IssueCategory Category => IssueCategory.Security;

    private static readonly Dictionary<string, string> WeakAlgorithms = new()
    {
        { "MD5", "MD5 is cryptographically broken and should not be used for security purposes." },
        { "SHA1", "SHA1 is considered weak and should not be used for security purposes." },
        { "DES", "DES is obsolete and provides insufficient security. Use AES instead." },
        { "TripleDES", "TripleDES is deprecated. Use AES instead." },
        { "3DES", "3DES is deprecated. Use AES instead." },
        { "RC2", "RC2 is weak and should not be used." },
        { "RC4", "RC4 is broken and should not be used." },
        { "Rijndael", "Use AES (AesManaged or Aes.Create()) instead of Rijndael directly." }
    };

    private static readonly HashSet<string> WeakKeyDerivation = new()
    {
        "Rfc2898DeriveBytes" // Check for low iteration counts
    };

    private static readonly int MinimumPbkdf2Iterations = 100000;
    private static readonly int MinimumAesKeySize = 128;

    public override Task<IEnumerable<AnalysisResult>> AnalyzeAsync(
        SyntaxTree syntaxTree,
        SemanticModel? semanticModel,
        string filePath)
    {
        var results = new List<AnalysisResult>();
        var root = syntaxTree.GetRoot();

        // Check for weak algorithm usage
        var objectCreations = root.DescendantNodes().OfType<ObjectCreationExpressionSyntax>();
        foreach (var creation in objectCreations)
        {
            var typeName = creation.Type.ToString();

            foreach (var (weakAlgo, message) in WeakAlgorithms)
            {
                if (typeName.Contains(weakAlgo, StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(CreateResult(
                        "SEC008",
                        $"Weak Cryptographic Algorithm: {weakAlgo}",
                        message,
                        filePath,
                        creation.GetLocation(),
                        Severity.Critical,
                        GetCodeSnippet(creation),
                        "Use strong algorithms: AES for encryption, SHA-256 or SHA-3 for hashing.",
                        "CWE-327",
                        "A02:2021 - Cryptographic Failures"));
                }
            }

            // Check Rfc2898DeriveBytes iteration count
            if (typeName.Contains("Rfc2898DeriveBytes"))
            {
                var args = creation.ArgumentList?.Arguments;
                if (args != null && args.Value.Count >= 3)
                {
                    var iterationArg = args.Value[2].Expression;
                    if (iterationArg is LiteralExpressionSyntax literal &&
                        literal.IsKind(SyntaxKind.NumericLiteralExpression))
                    {
                        if (int.TryParse(literal.Token.ValueText, out int iterations) &&
                            iterations < MinimumPbkdf2Iterations)
                        {
                            results.Add(CreateResult(
                                "SEC008",
                                "Weak PBKDF2 Iteration Count",
                                $"PBKDF2 iteration count ({iterations}) is below recommended minimum ({MinimumPbkdf2Iterations}).",
                                filePath,
                                creation.GetLocation(),
                                Severity.Critical,
                                GetCodeSnippet(creation),
                                $"Use at least {MinimumPbkdf2Iterations} iterations for PBKDF2.",
                                "CWE-916",
                                "A02:2021 - Cryptographic Failures"));
                        }
                    }
                }
            }
        }

        // Check for Create() factory methods with weak algorithms
        var invocations = root.DescendantNodes().OfType<InvocationExpressionSyntax>();
        foreach (var invocation in invocations)
        {
            var methodText = invocation.Expression.ToString();

            foreach (var (weakAlgo, message) in WeakAlgorithms)
            {
                if (methodText.Contains($"{weakAlgo}.Create", StringComparison.OrdinalIgnoreCase) ||
                    methodText.Contains($"{weakAlgo}Managed", StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(CreateResult(
                        "SEC008",
                        $"Weak Cryptographic Algorithm: {weakAlgo}",
                        message,
                        filePath,
                        invocation.GetLocation(),
                        Severity.Critical,
                        GetCodeSnippet(invocation),
                        "Use strong algorithms: AES for encryption, SHA-256 or SHA-3 for hashing.",
                        "CWE-327",
                        "A02:2021 - Cryptographic Failures"));
                }
            }

            // Check for HashAlgorithm.Create with weak algorithm name
            if (methodText.Contains("HashAlgorithm.Create") ||
                methodText.Contains("SymmetricAlgorithm.Create"))
            {
                var args = invocation.ArgumentList.Arguments;
                if (args.Any())
                {
                    var argValue = args.First().Expression.ToString().ToUpperInvariant();
                    if (argValue.Contains("MD5") || argValue.Contains("SHA1") ||
                        argValue.Contains("DES") || argValue.Contains("RC"))
                    {
                        results.Add(CreateResult(
                            "SEC008",
                            "Weak Algorithm Specified by Name",
                            "A weak cryptographic algorithm is being created by name.",
                            filePath,
                            invocation.GetLocation(),
                            Severity.Critical,
                            GetCodeSnippet(invocation),
                            "Use strong algorithm names like 'SHA256', 'SHA384', or 'AES'.",
                            "CWE-327",
                            "A02:2021 - Cryptographic Failures"));
                    }
                }
            }
        }

        // Check for weak key sizes
        var assignments = root.DescendantNodes().OfType<AssignmentExpressionSyntax>();
        foreach (var assignment in assignments)
        {
            var leftText = assignment.Left.ToString();

            if (leftText.Contains("KeySize") || leftText.Contains("BlockSize"))
            {
                if (assignment.Right is LiteralExpressionSyntax literal &&
                    literal.IsKind(SyntaxKind.NumericLiteralExpression))
                {
                    if (int.TryParse(literal.Token.ValueText, out int keySize))
                    {
                        if (leftText.Contains("KeySize") && keySize < MinimumAesKeySize)
                        {
                            results.Add(CreateResult(
                                "SEC008",
                                "Weak Key Size",
                                $"Key size ({keySize} bits) is below minimum recommended size ({MinimumAesKeySize} bits).",
                                filePath,
                                assignment.GetLocation(),
                                Severity.Critical,
                                GetCodeSnippet(assignment),
                                $"Use a key size of at least {MinimumAesKeySize} bits (preferably 256 bits).",
                                "CWE-326",
                                "A02:2021 - Cryptographic Failures"));
                        }
                    }
                }
            }
        }

        // Check for ECB mode usage
        foreach (var assignment in assignments)
        {
            var rightText = assignment.Right.ToString();
            if (rightText.Contains("CipherMode.ECB"))
            {
                results.Add(CreateResult(
                    "SEC008",
                    "Insecure ECB Cipher Mode",
                    "ECB mode does not provide semantic security and should not be used.",
                    filePath,
                    assignment.GetLocation(),
                    Severity.Critical,
                    GetCodeSnippet(assignment),
                    "Use CBC, GCM, or other secure cipher modes instead of ECB.",
                    "CWE-327",
                    "A02:2021 - Cryptographic Failures"));
            }
        }

        return Task.FromResult<IEnumerable<AnalysisResult>>(results);
    }
}
