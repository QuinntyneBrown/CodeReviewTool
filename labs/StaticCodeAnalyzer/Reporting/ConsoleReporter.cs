using StaticCodeAnalyzer.Analysis;
using Spectre.Console;

namespace StaticCodeAnalyzer.Reporting;

public class ConsoleReporter
{
    public void GenerateReport(List<AnalysisResult> results)
    {
        if (!results.Any())
        {
            AnsiConsole.MarkupLine("\n[green]No issues found![/]");
            return;
        }

        // Summary statistics
        PrintSummary(results);

        // Group by severity
        PrintBySeverity(results);

        // Group by category
        PrintByCategory(results);

        // Detailed issues table
        PrintDetailedIssues(results);

        // Security-specific summary (important for safety-critical)
        PrintSecuritySummary(results);
    }

    private void PrintSummary(List<AnalysisResult> results)
    {
        var table = new Table();
        table.Title = new TableTitle("[bold]Analysis Summary[/]");
        table.Border = TableBorder.Rounded;

        table.AddColumn("Metric");
        table.AddColumn("Value");

        var filesAnalyzed = results.Select(r => r.FilePath).Distinct().Count();
        var totalIssues = results.Count;

        table.AddRow("Files with issues", filesAnalyzed.ToString());
        table.AddRow("Total issues", totalIssues.ToString());
        table.AddRow("[red]Critical/Blocker[/]", results.Count(r => r.Severity == Severity.Critical || r.Severity == Severity.Blocker).ToString());
        table.AddRow("[yellow]Major[/]", results.Count(r => r.Severity == Severity.Major).ToString());
        table.AddRow("[blue]Minor[/]", results.Count(r => r.Severity == Severity.Minor).ToString());
        table.AddRow("[dim]Info[/]", results.Count(r => r.Severity == Severity.Info).ToString());

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    private void PrintBySeverity(List<AnalysisResult> results)
    {
        var chart = new BreakdownChart()
            .Width(60)
            .AddItem("Blocker", results.Count(r => r.Severity == Severity.Blocker), Color.Maroon)
            .AddItem("Critical", results.Count(r => r.Severity == Severity.Critical), Color.Red)
            .AddItem("Major", results.Count(r => r.Severity == Severity.Major), Color.Yellow)
            .AddItem("Minor", results.Count(r => r.Severity == Severity.Minor), Color.Blue)
            .AddItem("Info", results.Count(r => r.Severity == Severity.Info), Color.Grey);

        AnsiConsole.Write(new Panel(chart)
            .Header("[bold]Issues by Severity[/]")
            .Border(BoxBorder.Rounded));
        AnsiConsole.WriteLine();
    }

    private void PrintByCategory(List<AnalysisResult> results)
    {
        var chart = new BreakdownChart()
            .Width(60)
            .AddItem("Security", results.Count(r => r.Category == IssueCategory.Security), Color.Red)
            .AddItem("Reliability", results.Count(r => r.Category == IssueCategory.Reliability), Color.Orange1)
            .AddItem("Maintainability", results.Count(r => r.Category == IssueCategory.Maintainability), Color.Yellow)
            .AddItem("Code Smell", results.Count(r => r.Category == IssueCategory.CodeSmell), Color.Blue)
            .AddItem("Bug", results.Count(r => r.Category == IssueCategory.Bug), Color.Purple);

        AnsiConsole.Write(new Panel(chart)
            .Header("[bold]Issues by Category[/]")
            .Border(BoxBorder.Rounded));
        AnsiConsole.WriteLine();
    }

    private void PrintDetailedIssues(List<AnalysisResult> results)
    {
        // Group by file and show top issues
        var groupedByFile = results
            .GroupBy(r => r.FilePath)
            .OrderByDescending(g => g.Count(r => r.Severity == Severity.Critical || r.Severity == Severity.Blocker))
            .ThenByDescending(g => g.Count());

        AnsiConsole.Write(new Rule("[bold]Detailed Issues[/]") { Justification = Justify.Left });
        AnsiConsole.WriteLine();

        foreach (var fileGroup in groupedByFile.Take(20)) // Limit output
        {
            var fileName = Path.GetFileName(fileGroup.Key);
            var criticalCount = fileGroup.Count(r => r.Severity == Severity.Critical || r.Severity == Severity.Blocker);

            var headerColor = criticalCount > 0 ? "red" : "yellow";
            AnsiConsole.MarkupLine($"[{headerColor} bold]{Markup.Escape(fileName)}[/] ({fileGroup.Count()} issues)");

            var issuesTable = new Table();
            issuesTable.Border = TableBorder.Simple;
            issuesTable.AddColumn("Line");
            issuesTable.AddColumn("Severity");
            issuesTable.AddColumn("Rule");
            issuesTable.AddColumn("Issue");

            foreach (var issue in fileGroup.OrderByDescending(i => i.Severity).Take(10))
            {
                var severityMarkup = GetSeverityMarkup(issue.Severity);
                issuesTable.AddRow(
                    issue.LineNumber.ToString(),
                    severityMarkup,
                    issue.RuleId,
                    Markup.Escape(TruncateText(issue.Title, 50)));
            }

            if (fileGroup.Count() > 10)
            {
                issuesTable.AddRow("...", "", "", $"[dim]+{fileGroup.Count() - 10} more issues[/]");
            }

            AnsiConsole.Write(issuesTable);
            AnsiConsole.WriteLine();
        }

        if (groupedByFile.Count() > 20)
        {
            AnsiConsole.MarkupLine($"[dim]... and {groupedByFile.Count() - 20} more files with issues[/]");
        }
    }

    private void PrintSecuritySummary(List<AnalysisResult> results)
    {
        var securityIssues = results.Where(r => r.Category == IssueCategory.Security).ToList();

        if (!securityIssues.Any())
        {
            AnsiConsole.Write(new Panel("[green]No security vulnerabilities detected[/]")
                .Header("[bold green]Security Assessment[/]")
                .Border(BoxBorder.Double));
            return;
        }

        var panel = new Panel(new Rows(
            new Markup($"[red bold]Security issues found: {securityIssues.Count}[/]"),
            new Markup(""),
            new Markup("[bold]Top Security Concerns:[/]")
        ));

        AnsiConsole.Write(new Panel($"[red bold]SECURITY ISSUES FOUND: {securityIssues.Count}[/]")
            .Header("[bold red]Security Assessment[/]")
            .Border(BoxBorder.Double));

        // Group by CWE
        var byCwe = securityIssues
            .Where(s => !string.IsNullOrEmpty(s.CweId))
            .GroupBy(s => s.CweId)
            .OrderByDescending(g => g.Count());

        if (byCwe.Any())
        {
            var cweTable = new Table();
            cweTable.Title = new TableTitle("[bold]Security Issues by CWE[/]");
            cweTable.Border = TableBorder.Rounded;
            cweTable.AddColumn("CWE");
            cweTable.AddColumn("Count");
            cweTable.AddColumn("Severity");

            foreach (var cweGroup in byCwe.Take(10))
            {
                var maxSeverity = cweGroup.Max(s => s.Severity);
                cweTable.AddRow(
                    cweGroup.Key ?? "Unknown",
                    cweGroup.Count().ToString(),
                    GetSeverityMarkup(maxSeverity));
            }

            AnsiConsole.Write(cweTable);
        }

        // Group by OWASP
        var byOwasp = securityIssues
            .Where(s => !string.IsNullOrEmpty(s.OwaspCategory))
            .GroupBy(s => s.OwaspCategory)
            .OrderByDescending(g => g.Count());

        if (byOwasp.Any())
        {
            AnsiConsole.WriteLine();
            var owaspTable = new Table();
            owaspTable.Title = new TableTitle("[bold]OWASP Top 10 Coverage[/]");
            owaspTable.Border = TableBorder.Rounded;
            owaspTable.AddColumn("Category");
            owaspTable.AddColumn("Issues");

            foreach (var owaspGroup in byOwasp)
            {
                owaspTable.AddRow(
                    Markup.Escape(owaspGroup.Key ?? "Unknown"),
                    owaspGroup.Count().ToString());
            }

            AnsiConsole.Write(owaspTable);
        }

        // List critical security issues
        var criticalSecurity = securityIssues
            .Where(s => s.Severity == Severity.Critical || s.Severity == Severity.Blocker)
            .ToList();

        if (criticalSecurity.Any())
        {
            AnsiConsole.WriteLine();
            AnsiConsole.Write(new Rule("[red bold]Critical Security Issues - Immediate Action Required[/]"));

            foreach (var issue in criticalSecurity.Take(10))
            {
                var fileName = Path.GetFileName(issue.FilePath);
                AnsiConsole.MarkupLine($"  [red]•[/] {Markup.Escape(issue.Title)}");
                AnsiConsole.MarkupLine($"    [dim]{Markup.Escape(fileName)}:{issue.LineNumber}[/]");
                if (!string.IsNullOrEmpty(issue.Suggestion))
                {
                    AnsiConsole.MarkupLine($"    [green]Fix:[/] {Markup.Escape(TruncateText(issue.Suggestion, 80))}");
                }
                AnsiConsole.WriteLine();
            }

            if (criticalSecurity.Count > 10)
            {
                AnsiConsole.MarkupLine($"[dim]... and {criticalSecurity.Count - 10} more critical security issues[/]");
            }
        }
    }

    private static string GetSeverityMarkup(Severity severity)
    {
        return severity switch
        {
            Severity.Blocker => "[maroon bold]BLOCKER[/]",
            Severity.Critical => "[red bold]CRITICAL[/]",
            Severity.Major => "[yellow]MAJOR[/]",
            Severity.Minor => "[blue]MINOR[/]",
            Severity.Info => "[dim]INFO[/]",
            _ => severity.ToString()
        };
    }

    private static string TruncateText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text))
            return "";

        text = text.Replace("\r\n", " ").Replace("\n", " ");

        return text.Length > maxLength
            ? text.Substring(0, maxLength - 3) + "..."
            : text;
    }
}
