using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StaticCodeAnalyzer.Analysis.Analyzers.CodeSmells;

public class MagicNumberAnalyzer : BaseCodeAnalyzer
{
    public override string AnalyzerId => "SMELL002";
    public override string Name => "Magic Number Analyzer";
    public override IssueCategory Category => IssueCategory.CodeSmell;

    // Common acceptable literals
    private static readonly HashSet<string> AcceptableLiterals = new()
    {
        "0", "1", "-1", "2", "10", "100", "1000",
        "0.0", "1.0", "0.5", "100.0",
        "0f", "1f", "0.0f", "1.0f",
        "0d", "1d", "0.0d", "1.0d",
        "0L", "1L", "-1L",
        "0m", "1m"
    };

    public override Task<IEnumerable<AnalysisResult>> AnalyzeAsync(
        SyntaxTree syntaxTree,
        SemanticModel? semanticModel,
        string filePath)
    {
        var results = new List<AnalysisResult>();
        var root = syntaxTree.GetRoot();

        var numericLiterals = root.DescendantNodes()
            .OfType<LiteralExpressionSyntax>()
            .Where(l => l.IsKind(SyntaxKind.NumericLiteralExpression));

        foreach (var literal in numericLiterals)
        {
            var value = literal.Token.Text;

            // Skip acceptable common values
            if (AcceptableLiterals.Contains(value))
                continue;

            // Skip if in constant/readonly declaration
            if (IsInConstantDeclaration(literal))
                continue;

            // Skip if in attribute
            if (literal.Ancestors().Any(a => a is AttributeSyntax))
                continue;

            // Skip if in enum
            if (literal.Ancestors().Any(a => a is EnumMemberDeclarationSyntax))
                continue;

            // Skip array size declarations for small arrays
            if (IsArraySizeDeclaration(literal) && IsSmallNumber(value))
                continue;

            // Skip in tests
            if (IsInTestContext(literal))
                continue;

            // Determine severity based on context
            var severity = Severity.Minor;
            var context = GetMagicNumberContext(literal);

            if (context.Contains("comparison") || context.Contains("condition"))
            {
                severity = Severity.Major; // Magic numbers in conditions are worse
            }

            results.Add(CreateResult(
                "SMELL002",
                "Magic Number",
                $"Magic number '{value}' found in {context}. Consider using a named constant.",
                filePath,
                literal.GetLocation(),
                severity,
                GetCodeSnippet(literal.Parent ?? literal),
                "Extract to a named constant with a descriptive name explaining its purpose."));
        }

        // Check for magic strings
        var stringLiterals = root.DescendantNodes()
            .OfType<LiteralExpressionSyntax>()
            .Where(l => l.IsKind(SyntaxKind.StringLiteralExpression));

        foreach (var literal in stringLiterals)
        {
            var value = literal.Token.ValueText;

            // Skip empty/short strings
            if (string.IsNullOrEmpty(value) || value.Length < 3)
                continue;

            // Skip if in constant declaration
            if (IsInConstantDeclaration(literal))
                continue;

            // Skip common acceptable patterns
            if (IsAcceptableString(value))
                continue;

            // Skip if in attribute
            if (literal.Ancestors().Any(a => a is AttributeSyntax))
                continue;

            // Check for repeated strings
            var sameStrings = root.DescendantNodes()
                .OfType<LiteralExpressionSyntax>()
                .Where(l => l.IsKind(SyntaxKind.StringLiteralExpression) &&
                           l.Token.ValueText == value);

            if (sameStrings.Count() >= 3)
            {
                results.Add(CreateResult(
                    "SMELL002",
                    "Repeated Magic String",
                    $"String \"{TruncateString(value)}\" is used {sameStrings.Count()} times. Consider using a constant.",
                    filePath,
                    literal.GetLocation(),
                    Severity.Minor,
                    GetCodeSnippet(literal),
                    "Extract repeated strings to a named constant."));
            }

            // Check for likely configuration values
            if (LooksLikeConfigValue(value))
            {
                results.Add(CreateResult(
                    "SMELL002",
                    "Hardcoded Configuration Value",
                    $"String \"{TruncateString(value)}\" looks like a configuration value.",
                    filePath,
                    literal.GetLocation(),
                    Severity.Minor,
                    GetCodeSnippet(literal),
                    "Consider moving configuration values to config files or environment variables."));
            }
        }

        return Task.FromResult<IEnumerable<AnalysisResult>>(results);
    }

    private static bool IsInConstantDeclaration(LiteralExpressionSyntax literal)
    {
        var parent = literal.Parent;

        while (parent != null)
        {
            if (parent is FieldDeclarationSyntax field)
            {
                return field.Modifiers.Any(m =>
                    m.IsKind(SyntaxKind.ConstKeyword) ||
                    m.IsKind(SyntaxKind.ReadOnlyKeyword));
            }

            if (parent is LocalDeclarationStatementSyntax local)
            {
                return local.Modifiers.Any(m => m.IsKind(SyntaxKind.ConstKeyword));
            }

            if (parent is VariableDeclaratorSyntax)
            {
                // Check if it's the initial assignment of a const/readonly
                var fieldDecl = parent.Ancestors().OfType<FieldDeclarationSyntax>().FirstOrDefault();
                if (fieldDecl != null)
                {
                    return fieldDecl.Modifiers.Any(m =>
                        m.IsKind(SyntaxKind.ConstKeyword) ||
                        m.IsKind(SyntaxKind.ReadOnlyKeyword));
                }
            }

            parent = parent.Parent;
        }

        return false;
    }

    private static bool IsArraySizeDeclaration(LiteralExpressionSyntax literal)
    {
        return literal.Parent is ArrayRankSpecifierSyntax ||
               literal.Ancestors().Any(a => a is ArrayCreationExpressionSyntax);
    }

    private static bool IsSmallNumber(string value)
    {
        if (int.TryParse(value, out int num))
        {
            return num <= 32 && num >= 0;
        }
        return false;
    }

    private static bool IsInTestContext(LiteralExpressionSyntax literal)
    {
        var method = literal.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
        if (method == null)
            return false;

        // Check for test attributes
        var hasTestAttribute = method.AttributeLists
            .SelectMany(a => a.Attributes)
            .Any(a =>
            {
                var name = a.Name.ToString();
                return name.Contains("Test") || name.Contains("Fact") ||
                       name.Contains("Theory") || name.Contains("TestMethod");
            });

        return hasTestAttribute;
    }

    private static string GetMagicNumberContext(LiteralExpressionSyntax literal)
    {
        var parent = literal.Parent;

        if (parent is BinaryExpressionSyntax binary)
        {
            if (binary.IsKind(SyntaxKind.EqualsExpression) ||
                binary.IsKind(SyntaxKind.NotEqualsExpression) ||
                binary.IsKind(SyntaxKind.LessThanExpression) ||
                binary.IsKind(SyntaxKind.GreaterThanExpression))
            {
                return "comparison";
            }
        }

        if (literal.Ancestors().Any(a => a is IfStatementSyntax || a is WhileStatementSyntax))
        {
            return "condition";
        }

        if (literal.Ancestors().Any(a => a is ArgumentSyntax))
        {
            return "method argument";
        }

        if (literal.Ancestors().Any(a => a is AssignmentExpressionSyntax))
        {
            return "assignment";
        }

        return "code";
    }

    private static bool IsAcceptableString(string value)
    {
        // Common acceptable patterns
        return value == " " ||
               value == ", " ||
               value == "." ||
               value == ":" ||
               value == "\n" ||
               value == "\r\n" ||
               value == "/" ||
               value.StartsWith("{") && value.EndsWith("}") || // Format strings
               value.All(c => char.IsPunctuation(c) || char.IsWhiteSpace(c));
    }

    private static bool LooksLikeConfigValue(string value)
    {
        return value.Contains("://") || // URLs
               value.Contains("\\\\") || // UNC paths
               value.Contains("@") && value.Contains(".") || // Emails
               value.StartsWith("http") ||
               value.StartsWith("ftp") ||
               value.EndsWith(".json") ||
               value.EndsWith(".xml") ||
               value.EndsWith(".config");
    }

    private static string TruncateString(string value)
    {
        const int maxLength = 30;
        return value.Length > maxLength
            ? value.Substring(0, maxLength) + "..."
            : value;
    }
}
