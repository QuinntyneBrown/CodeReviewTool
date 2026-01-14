using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace StaticCodeAnalyzer.Analysis;

public interface ICodeAnalyzer
{
    string AnalyzerId { get; }
    string Name { get; }
    IssueCategory Category { get; }
    Task<IEnumerable<AnalysisResult>> AnalyzeAsync(SyntaxTree syntaxTree, SemanticModel? semanticModel, string filePath);
}

public abstract class BaseCodeAnalyzer : ICodeAnalyzer
{
    public abstract string AnalyzerId { get; }
    public abstract string Name { get; }
    public abstract IssueCategory Category { get; }

    public abstract Task<IEnumerable<AnalysisResult>> AnalyzeAsync(
        SyntaxTree syntaxTree,
        SemanticModel? semanticModel,
        string filePath);

    protected AnalysisResult CreateResult(
        string ruleId,
        string title,
        string description,
        string filePath,
        Location location,
        Severity severity,
        string? codeSnippet = null,
        string? suggestion = null,
        string? cweId = null,
        string? owaspCategory = null)
    {
        var lineSpan = location.GetLineSpan();
        return new AnalysisResult
        {
            RuleId = ruleId,
            Title = title,
            Description = description,
            FilePath = filePath,
            LineNumber = lineSpan.StartLinePosition.Line + 1,
            ColumnNumber = lineSpan.StartLinePosition.Character + 1,
            Severity = severity,
            Category = Category,
            CodeSnippet = codeSnippet,
            Suggestion = suggestion,
            CweId = cweId,
            OwaspCategory = owaspCategory
        };
    }

    protected static string GetCodeSnippet(SyntaxNode node, int maxLength = 100)
    {
        var text = node.ToString();
        if (text.Length > maxLength)
        {
            text = text.Substring(0, maxLength) + "...";
        }
        return text.Replace("\r\n", " ").Replace("\n", " ").Trim();
    }
}
