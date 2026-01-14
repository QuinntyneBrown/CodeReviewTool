using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StaticCodeAnalyzer.Analysis.Analyzers.Security;

public class XxeAnalyzer : BaseCodeAnalyzer
{
    public override string AnalyzerId => "SEC007";
    public override string Name => "XML External Entity (XXE) Analyzer";
    public override IssueCategory Category => IssueCategory.Security;

    private static readonly HashSet<string> XmlTypes = new()
    {
        "XmlDocument", "XmlTextReader", "XmlReader", "XPathDocument",
        "XslCompiledTransform", "XmlSchema", "XmlReaderSettings"
    };

    public override Task<IEnumerable<AnalysisResult>> AnalyzeAsync(
        SyntaxTree syntaxTree,
        SemanticModel? semanticModel,
        string filePath)
    {
        var results = new List<AnalysisResult>();
        var root = syntaxTree.GetRoot();

        // Track XmlReaderSettings configurations
        var safeReaderSettings = new HashSet<string>();

        // First pass: identify safe XmlReaderSettings
        var assignments = root.DescendantNodes().OfType<AssignmentExpressionSyntax>().ToList();
        foreach (var assignment in assignments)
        {
            var leftText = assignment.Left.ToString();
            var rightText = assignment.Right.ToString();

            if (leftText.Contains("DtdProcessing") && rightText.Contains("Prohibit"))
            {
                var variableName = GetParentVariableName(assignment);
                if (!string.IsNullOrEmpty(variableName))
                    safeReaderSettings.Add(variableName);
            }

            if (leftText.Contains("XmlResolver") && rightText == "null")
            {
                var variableName = GetParentVariableName(assignment);
                if (!string.IsNullOrEmpty(variableName))
                    safeReaderSettings.Add(variableName);
            }
        }

        // Check XmlDocument usage
        var objectCreations = root.DescendantNodes().OfType<ObjectCreationExpressionSyntax>();
        foreach (var creation in objectCreations)
        {
            var typeName = creation.Type.ToString();

            if (typeName.Contains("XmlDocument"))
            {
                // Check if XmlResolver is being set to null later
                var parentMethod = creation.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
                if (parentMethod != null)
                {
                    var methodAssignments = parentMethod.DescendantNodes()
                        .OfType<AssignmentExpressionSyntax>()
                        .Where(a => a.Left.ToString().Contains("XmlResolver") &&
                                   a.Right.ToString() == "null");

                    if (!methodAssignments.Any())
                    {
                        results.Add(CreateResult(
                            "SEC007",
                            "Potential XXE Vulnerability in XmlDocument",
                            "XmlDocument created without setting XmlResolver to null. This may allow XXE attacks.",
                            filePath,
                            creation.GetLocation(),
                            Severity.Critical,
                            GetCodeSnippet(creation),
                            "Set XmlResolver = null to prevent XXE attacks.",
                            "CWE-611",
                            "A05:2021 - Security Misconfiguration"));
                    }
                }
            }

            if (typeName.Contains("XmlTextReader"))
            {
                results.Add(CreateResult(
                    "SEC007",
                    "Deprecated XmlTextReader Usage",
                    "XmlTextReader is deprecated and vulnerable to XXE by default. Use XmlReader.Create with secure settings.",
                    filePath,
                    creation.GetLocation(),
                    Severity.Critical,
                    GetCodeSnippet(creation),
                    "Use XmlReader.Create() with XmlReaderSettings that has DtdProcessing = DtdProcessing.Prohibit.",
                    "CWE-611",
                    "A05:2021 - Security Misconfiguration"));
            }
        }

        // Check XmlReader.Create calls
        var invocations = root.DescendantNodes().OfType<InvocationExpressionSyntax>();
        foreach (var invocation in invocations)
        {
            var methodText = invocation.Expression.ToString();

            if (methodText.Contains("XmlReader.Create"))
            {
                var args = invocation.ArgumentList.Arguments;

                // Check if settings are provided
                bool hasSecureSettings = args.Any(a =>
                {
                    var argText = a.Expression.ToString();
                    return safeReaderSettings.Contains(argText) ||
                           argText.Contains("DtdProcessing.Prohibit");
                });

                if (!hasSecureSettings && args.Count < 2)
                {
                    results.Add(CreateResult(
                        "SEC007",
                        "XmlReader Created Without Secure Settings",
                        "XmlReader.Create called without explicit secure XmlReaderSettings.",
                        filePath,
                        invocation.GetLocation(),
                        Severity.Major,
                        GetCodeSnippet(invocation),
                        "Provide XmlReaderSettings with DtdProcessing = DtdProcessing.Prohibit and XmlResolver = null.",
                        "CWE-611",
                        "A05:2021 - Security Misconfiguration"));
                }
            }

            // Check for LoadXml with user input
            if (methodText.Contains("LoadXml") || methodText.Contains("Load"))
            {
                var args = invocation.ArgumentList.Arguments;
                if (args.Any(a => IsUserControlledInput(a.Expression)))
                {
                    results.Add(CreateResult(
                        "SEC007",
                        "XML Loading from User Input",
                        "XML being loaded from potentially untrusted source without visible XXE protections.",
                        filePath,
                        invocation.GetLocation(),
                        Severity.Critical,
                        GetCodeSnippet(invocation),
                        "Ensure XXE protections are in place when loading XML from untrusted sources.",
                        "CWE-611",
                        "A05:2021 - Security Misconfiguration"));
                }
            }
        }

        // Check for DtdProcessing.Parse which is dangerous
        foreach (var assignment in assignments)
        {
            var rightText = assignment.Right.ToString();
            if (rightText.Contains("DtdProcessing.Parse"))
            {
                results.Add(CreateResult(
                    "SEC007",
                    "Dangerous DtdProcessing Setting",
                    "DtdProcessing.Parse enables DTD processing which can lead to XXE attacks.",
                    filePath,
                    assignment.GetLocation(),
                    Severity.Critical,
                    GetCodeSnippet(assignment),
                    "Use DtdProcessing.Prohibit instead of DtdProcessing.Parse.",
                    "CWE-611",
                    "A05:2021 - Security Misconfiguration"));
            }
        }

        return Task.FromResult<IEnumerable<AnalysisResult>>(results);
    }

    private static string GetParentVariableName(AssignmentExpressionSyntax assignment)
    {
        if (assignment.Left is MemberAccessExpressionSyntax memberAccess)
        {
            if (memberAccess.Expression is IdentifierNameSyntax identifier)
            {
                return identifier.Identifier.Text;
            }
        }
        return string.Empty;
    }

    private static bool IsUserControlledInput(ExpressionSyntax expression)
    {
        var text = expression.ToString().ToLowerInvariant();
        var inputIndicators = new[] { "request", "input", "stream", "content", "body", "param", "user", "data" };
        return inputIndicators.Any(ind => text.Contains(ind));
    }
}
