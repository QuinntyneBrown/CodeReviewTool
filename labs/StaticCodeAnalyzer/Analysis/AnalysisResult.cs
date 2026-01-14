namespace StaticCodeAnalyzer.Analysis;

public enum Severity
{
    Info,
    Minor,
    Major,
    Critical,
    Blocker
}

public enum IssueCategory
{
    Security,
    Reliability,
    Maintainability,
    CodeSmell,
    Bug
}

public class AnalysisResult
{
    public required string RuleId { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required string FilePath { get; init; }
    public required int LineNumber { get; init; }
    public required int ColumnNumber { get; init; }
    public required Severity Severity { get; init; }
    public required IssueCategory Category { get; init; }
    public string? CodeSnippet { get; init; }
    public string? Suggestion { get; init; }
    public string? CweId { get; init; }
    public string? OwaspCategory { get; init; }

    public override string ToString() =>
        $"[{Severity}] {RuleId}: {Title} at {FilePath}:{LineNumber}:{ColumnNumber}";
}
