using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StaticCodeAnalyzer.Analysis.Analyzers.Security;

public class CommandInjectionAnalyzer : BaseCodeAnalyzer
{
    public override string AnalyzerId => "SEC002";
    public override string Name => "Command Injection Analyzer";
    public override IssueCategory Category => IssueCategory.Security;

    private static readonly HashSet<string> DangerousTypes = new()
    {
        "Process", "ProcessStartInfo"
    };

    private static readonly HashSet<string> DangerousMembers = new()
    {
        "FileName", "Arguments", "Start"
    };

    private static readonly HashSet<string> ShellMethods = new()
    {
        "cmd", "powershell", "bash", "sh", "/c", "-c", "-Command"
    };

    public override Task<IEnumerable<AnalysisResult>> AnalyzeAsync(
        SyntaxTree syntaxTree,
        SemanticModel? semanticModel,
        string filePath)
    {
        var results = new List<AnalysisResult>();
        var root = syntaxTree.GetRoot();

        // Check for Process.Start with dynamic arguments
        var invocations = root.DescendantNodes().OfType<InvocationExpressionSyntax>();
        foreach (var invocation in invocations)
        {
            if (IsProcessStartCall(invocation))
            {
                var args = invocation.ArgumentList.Arguments;
                foreach (var arg in args)
                {
                    if (IsDynamicInput(arg.Expression))
                    {
                        results.Add(CreateResult(
                            "SEC002",
                            "Potential Command Injection",
                            "Process.Start called with dynamic input. This may allow command injection attacks.",
                            filePath,
                            invocation.GetLocation(),
                            Severity.Critical,
                            GetCodeSnippet(invocation),
                            "Validate and sanitize all input used in process execution. Avoid shell execution when possible.",
                            "CWE-78",
                            "A03:2021 - Injection"));
                        break;
                    }
                }

                // Check for shell invocation patterns
                var argText = invocation.ArgumentList.ToString().ToLowerInvariant();
                if (ShellMethods.Any(s => argText.Contains(s.ToLowerInvariant())))
                {
                    results.Add(CreateResult(
                        "SEC002",
                        "Shell Command Execution Detected",
                        "Direct shell execution detected. This pattern is highly susceptible to command injection.",
                        filePath,
                        invocation.GetLocation(),
                        Severity.Critical,
                        GetCodeSnippet(invocation),
                        "Avoid shell execution. Use direct process invocation with explicit arguments.",
                        "CWE-78",
                        "A03:2021 - Injection"));
                }
            }
        }

        // Check for ProcessStartInfo assignments with dynamic values
        var assignments = root.DescendantNodes().OfType<AssignmentExpressionSyntax>();
        foreach (var assignment in assignments)
        {
            if (assignment.Left is MemberAccessExpressionSyntax memberAccess)
            {
                var memberName = memberAccess.Name.Identifier.Text;
                if (DangerousMembers.Contains(memberName))
                {
                    if (IsDynamicInput(assignment.Right))
                    {
                        results.Add(CreateResult(
                            "SEC002",
                            "Potential Command Injection via ProcessStartInfo",
                            $"ProcessStartInfo.{memberName} set with dynamic value. This may allow command injection.",
                            filePath,
                            assignment.GetLocation(),
                            Severity.Critical,
                            GetCodeSnippet(assignment),
                            "Validate and sanitize all input used in process configuration.",
                            "CWE-78",
                            "A03:2021 - Injection"));
                    }
                }
            }
        }

        return Task.FromResult<IEnumerable<AnalysisResult>>(results);
    }

    private static bool IsProcessStartCall(InvocationExpressionSyntax invocation)
    {
        var text = invocation.Expression.ToString();
        return text.Contains("Process.Start") || text.Contains("process.Start");
    }

    private static bool IsDynamicInput(ExpressionSyntax expression)
    {
        // String concatenation
        if (expression is BinaryExpressionSyntax binary && binary.IsKind(SyntaxKind.AddExpression))
            return true;

        // String interpolation
        if (expression is InterpolatedStringExpressionSyntax)
            return true;

        // Variable reference (could be user input)
        if (expression is IdentifierNameSyntax)
        {
            var name = expression.ToString().ToLowerInvariant();
            var inputIndicators = new[] { "input", "param", "arg", "command", "cmd", "request", "user", "data" };
            return inputIndicators.Any(ind => name.Contains(ind));
        }

        // String.Format calls
        if (expression is InvocationExpressionSyntax inv)
        {
            var methodName = inv.Expression.ToString();
            return methodName.Contains("Format") || methodName.Contains("Concat");
        }

        return false;
    }
}
