using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text.RegularExpressions;

namespace StaticCodeAnalyzer.Analysis.Analyzers.Security;

public class HardcodedCredentialsAnalyzer : BaseCodeAnalyzer
{
    public override string AnalyzerId => "SEC005";
    public override string Name => "Hardcoded Credentials Analyzer";
    public override IssueCategory Category => IssueCategory.Security;

    private static readonly HashSet<string> SensitiveVariableNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "password", "passwd", "pwd", "secret", "apikey", "api_key", "apiSecret", "api_secret",
        "token", "accesstoken", "access_token", "refreshtoken", "refresh_token",
        "privatekey", "private_key", "secretkey", "secret_key",
        "connectionstring", "connection_string", "connstring", "conn_string",
        "credentials", "auth", "authtoken", "auth_token", "bearer",
        "clientsecret", "client_secret", "appSecret", "app_secret"
    };

    private static readonly Regex[] SecretPatterns = new[]
    {
        new Regex(@"(?i)(password|passwd|pwd)\s*[:=]\s*[""'][^""']{4,}[""']", RegexOptions.Compiled),
        new Regex(@"(?i)(api[_-]?key|apikey)\s*[:=]\s*[""'][^""']{10,}[""']", RegexOptions.Compiled),
        new Regex(@"(?i)(secret|token)\s*[:=]\s*[""'][^""']{8,}[""']", RegexOptions.Compiled),
        new Regex(@"(?i)bearer\s+[a-zA-Z0-9\-_.]+", RegexOptions.Compiled),
        new Regex(@"(?i)(aws|azure|gcp)[_-]?(access|secret|key)", RegexOptions.Compiled),
        new Regex(@"-----BEGIN\s+(RSA\s+)?PRIVATE\s+KEY-----", RegexOptions.Compiled),
        new Regex(@"(?i)mongodb(\+srv)?://[^:]+:[^@]+@", RegexOptions.Compiled),
        new Regex(@"(?i)(mysql|postgres|sqlserver)://[^:]+:[^@]+@", RegexOptions.Compiled)
    };

    public override Task<IEnumerable<AnalysisResult>> AnalyzeAsync(
        SyntaxTree syntaxTree,
        SemanticModel? semanticModel,
        string filePath)
    {
        var results = new List<AnalysisResult>();
        var root = syntaxTree.GetRoot();

        // Check variable declarations with sensitive names
        var variableDeclarations = root.DescendantNodes().OfType<VariableDeclaratorSyntax>();
        foreach (var variable in variableDeclarations)
        {
            var varName = variable.Identifier.Text;
            if (IsSensitiveVariableName(varName) && variable.Initializer != null)
            {
                var initializer = variable.Initializer.Value;
                if (IsHardcodedValue(initializer))
                {
                    results.Add(CreateResult(
                        "SEC005",
                        "Hardcoded Credential Detected",
                        $"Variable '{varName}' appears to contain a hardcoded credential.",
                        filePath,
                        variable.GetLocation(),
                        Severity.Critical,
                        GetCodeSnippet(variable),
                        "Use environment variables, secure configuration, or a secrets manager.",
                        "CWE-798",
                        "A07:2021 - Identification and Authentication Failures"));
                }
            }
        }

        // Check property assignments
        var assignments = root.DescendantNodes().OfType<AssignmentExpressionSyntax>();
        foreach (var assignment in assignments)
        {
            var leftName = GetAssignmentTargetName(assignment.Left);
            if (IsSensitiveVariableName(leftName) && IsHardcodedValue(assignment.Right))
            {
                results.Add(CreateResult(
                    "SEC005",
                    "Hardcoded Credential in Assignment",
                    $"Property or field '{leftName}' assigned a hardcoded credential value.",
                    filePath,
                    assignment.GetLocation(),
                    Severity.Critical,
                    GetCodeSnippet(assignment),
                    "Use secure configuration providers instead of hardcoded values.",
                    "CWE-798",
                    "A07:2021 - Identification and Authentication Failures"));
            }
        }

        // Check string literals for secret patterns
        var stringLiterals = root.DescendantNodes().OfType<LiteralExpressionSyntax>()
            .Where(l => l.IsKind(SyntaxKind.StringLiteralExpression));

        foreach (var literal in stringLiterals)
        {
            var value = literal.Token.ValueText;
            if (ContainsSecretPattern(value))
            {
                results.Add(CreateResult(
                    "SEC005",
                    "Potential Secret in String Literal",
                    "String literal appears to contain a secret or credential pattern.",
                    filePath,
                    literal.GetLocation(),
                    Severity.Critical,
                    "[REDACTED - potential secret]",
                    "Move secrets to secure configuration or environment variables.",
                    "CWE-798",
                    "A07:2021 - Identification and Authentication Failures"));
            }
        }

        // Check for connection strings with embedded credentials
        var interpolatedStrings = root.DescendantNodes().OfType<InterpolatedStringExpressionSyntax>();
        foreach (var interpolated in interpolatedStrings)
        {
            var text = interpolated.ToString().ToLowerInvariant();
            if (text.Contains("password=") || text.Contains("pwd=") ||
                text.Contains("user id=") || text.Contains("uid="))
            {
                results.Add(CreateResult(
                    "SEC005",
                    "Connection String with Credentials",
                    "Connection string appears to contain embedded credentials.",
                    filePath,
                    interpolated.GetLocation(),
                    Severity.Critical,
                    GetCodeSnippet(interpolated),
                    "Use integrated security or store credentials in secure configuration.",
                    "CWE-798",
                    "A07:2021 - Identification and Authentication Failures"));
            }
        }

        return Task.FromResult<IEnumerable<AnalysisResult>>(results);
    }

    private static bool IsSensitiveVariableName(string name)
    {
        var lowerName = name.ToLowerInvariant();
        return SensitiveVariableNames.Any(s => lowerName.Contains(s.ToLowerInvariant()));
    }

    private static bool IsHardcodedValue(ExpressionSyntax expression)
    {
        return expression is LiteralExpressionSyntax literal &&
               literal.IsKind(SyntaxKind.StringLiteralExpression) &&
               !string.IsNullOrWhiteSpace(literal.Token.ValueText) &&
               literal.Token.ValueText.Length >= 4;
    }

    private static string GetAssignmentTargetName(ExpressionSyntax expression)
    {
        return expression switch
        {
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.Text,
            _ => string.Empty
        };
    }

    private static bool ContainsSecretPattern(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length < 8)
            return false;

        return SecretPatterns.Any(pattern => pattern.IsMatch(value));
    }
}
