using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StaticCodeAnalyzer.Analysis.Analyzers.Security;

public class InsecureDeserializationAnalyzer : BaseCodeAnalyzer
{
    public override string AnalyzerId => "SEC006";
    public override string Name => "Insecure Deserialization Analyzer";
    public override IssueCategory Category => IssueCategory.Security;

    private static readonly HashSet<string> DangerousSerializers = new()
    {
        "BinaryFormatter", "SoapFormatter", "NetDataContractSerializer",
        "LosFormatter", "ObjectStateFormatter", "JavaScriptSerializer"
    };

    private static readonly HashSet<string> DangerousMethods = new()
    {
        "Deserialize", "DeserializeObject", "UnsafeDeserialize",
        "ReadObject", "DeserializeFromString"
    };

    private static readonly HashSet<string> JsonSettings = new()
    {
        "TypeNameHandling"
    };

    public override Task<IEnumerable<AnalysisResult>> AnalyzeAsync(
        SyntaxTree syntaxTree,
        SemanticModel? semanticModel,
        string filePath)
    {
        var results = new List<AnalysisResult>();
        var root = syntaxTree.GetRoot();

        // Check for dangerous serializer usage
        var objectCreations = root.DescendantNodes().OfType<ObjectCreationExpressionSyntax>();
        foreach (var creation in objectCreations)
        {
            var typeName = creation.Type.ToString();
            if (DangerousSerializers.Any(s => typeName.Contains(s)))
            {
                results.Add(CreateResult(
                    "SEC006",
                    "Insecure Deserializer Usage",
                    $"{typeName} is vulnerable to deserialization attacks. Never deserialize untrusted data with this class.",
                    filePath,
                    creation.GetLocation(),
                    Severity.Critical,
                    GetCodeSnippet(creation),
                    "Use safer alternatives like System.Text.Json or Newtonsoft.Json with proper settings.",
                    "CWE-502",
                    "A08:2021 - Software and Data Integrity Failures"));
            }
        }

        // Check for deserialization of user input
        var invocations = root.DescendantNodes().OfType<InvocationExpressionSyntax>();
        foreach (var invocation in invocations)
        {
            var methodName = GetMethodName(invocation);

            if (DangerousMethods.Contains(methodName))
            {
                var fullText = invocation.ToString().ToLowerInvariant();

                // Check if using dangerous serializers
                if (DangerousSerializers.Any(s => fullText.Contains(s.ToLowerInvariant())))
                {
                    results.Add(CreateResult(
                        "SEC006",
                        "Insecure Deserialization Method",
                        "Deserialization with potentially dangerous serializer detected.",
                        filePath,
                        invocation.GetLocation(),
                        Severity.Critical,
                        GetCodeSnippet(invocation),
                        "Avoid deserializing untrusted data with BinaryFormatter or similar unsafe serializers.",
                        "CWE-502",
                        "A08:2021 - Software and Data Integrity Failures"));
                }

                // Check if deserializing user input
                var args = invocation.ArgumentList.Arguments;
                if (args.Any(a => IsUserControlledInput(a.Expression)))
                {
                    results.Add(CreateResult(
                        "SEC006",
                        "Deserialization of User-Controlled Data",
                        "Deserialization of potentially untrusted input detected.",
                        filePath,
                        invocation.GetLocation(),
                        Severity.Critical,
                        GetCodeSnippet(invocation),
                        "Validate and sanitize input before deserialization. Consider using a whitelist of allowed types.",
                        "CWE-502",
                        "A08:2021 - Software and Data Integrity Failures"));
                }
            }
        }

        // Check for dangerous TypeNameHandling in JSON settings
        var assignments = root.DescendantNodes().OfType<AssignmentExpressionSyntax>();
        foreach (var assignment in assignments)
        {
            var leftText = assignment.Left.ToString();
            var rightText = assignment.Right.ToString();

            if (leftText.Contains("TypeNameHandling") &&
                (rightText.Contains("All") || rightText.Contains("Auto") ||
                 rightText.Contains("Objects") || rightText.Contains("Arrays")))
            {
                results.Add(CreateResult(
                    "SEC006",
                    "Dangerous TypeNameHandling Setting",
                    "TypeNameHandling set to a value that allows type specification. This enables deserialization attacks.",
                    filePath,
                    assignment.GetLocation(),
                    Severity.Critical,
                    GetCodeSnippet(assignment),
                    "Use TypeNameHandling.None or implement a SerializationBinder to restrict allowed types.",
                    "CWE-502",
                    "A08:2021 - Software and Data Integrity Failures"));
            }
        }

        // Check for XML deserialization without type restrictions
        foreach (var invocation in invocations)
        {
            var text = invocation.ToString();
            if (text.Contains("XmlSerializer") && text.Contains("Deserialize"))
            {
                // XmlSerializer with type from user input
                var args = invocation.ArgumentList.Arguments;
                if (args.Any(a => IsUserControlledInput(a.Expression)))
                {
                    results.Add(CreateResult(
                        "SEC006",
                        "XML Deserialization with User Input",
                        "XmlSerializer deserializing potentially untrusted input.",
                        filePath,
                        invocation.GetLocation(),
                        Severity.Major,
                        GetCodeSnippet(invocation),
                        "Ensure the XmlSerializer type is not user-controlled. Validate input before deserialization.",
                        "CWE-502",
                        "A08:2021 - Software and Data Integrity Failures"));
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

    private static bool IsUserControlledInput(ExpressionSyntax expression)
    {
        var text = expression.ToString().ToLowerInvariant();
        var inputIndicators = new[] { "request", "input", "body", "stream", "data", "content", "param", "query" };
        return inputIndicators.Any(ind => text.Contains(ind));
    }
}
