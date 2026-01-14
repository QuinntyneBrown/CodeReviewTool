using StaticCodeAnalyzer.Analysis;
using StaticCodeAnalyzer.Reporting;
using Spectre.Console;

namespace StaticCodeAnalyzer;

class Program
{
    static async Task<int> Main(string[] args)
    {
        AnsiConsole.Write(new FigletText("Static Code Analyzer")
            .Color(Color.Blue));

        AnsiConsole.MarkupLine("[bold blue]Safety-Critical Static Code Analysis Tool[/]");
        AnsiConsole.MarkupLine("[dim]Equivalent to SonarQube Safety-Critical Profile[/]\n");

        string targetPath = args.Length > 0 ? args[0] : Directory.GetCurrentDirectory();

        if (!Directory.Exists(targetPath) && !File.Exists(targetPath))
        {
            AnsiConsole.MarkupLine($"[red]Error: Path not found: {targetPath}[/]");
            return 1;
        }

        AnsiConsole.MarkupLine($"[green]Analyzing:[/] {targetPath}\n");

        var analysisEngine = new AnalysisEngine();
        var results = new List<AnalysisResult>();

        await AnsiConsole.Status()
            .AutoRefresh(true)
            .Spinner(Spinner.Known.Dots)
            .StartAsync("[yellow]Running analysis...[/]", async ctx =>
            {
                ctx.Status("[yellow]Discovering files...[/]");
                var files = GetCSharpFiles(targetPath);

                AnsiConsole.MarkupLine($"[dim]Found {files.Count} C# files to analyze[/]");

                int current = 0;
                foreach (var file in files)
                {
                    current++;
                    ctx.Status($"[yellow]Analyzing file {current}/{files.Count}: {Path.GetFileName(file)}[/]");

                    try
                    {
                        var fileResults = await analysisEngine.AnalyzeFileAsync(file);
                        results.AddRange(fileResults);
                    }
                    catch (Exception ex)
                    {
                        AnsiConsole.MarkupLine($"[red]Error analyzing {file}: {ex.Message}[/]");
                    }
                }
            });

        var reporter = new ConsoleReporter();
        reporter.GenerateReport(results);

        int criticalCount = results.Count(r => r.Severity == Severity.Critical);
        int majorCount = results.Count(r => r.Severity == Severity.Major);

        if (criticalCount > 0)
        {
            AnsiConsole.MarkupLine($"\n[red bold]Analysis failed: {criticalCount} critical issues found[/]");
            return 2;
        }

        if (majorCount > 0)
        {
            AnsiConsole.MarkupLine($"\n[yellow bold]Analysis completed with warnings: {majorCount} major issues found[/]");
            return 1;
        }

        AnsiConsole.MarkupLine("\n[green bold]Analysis passed: No critical or major issues found[/]");
        return 0;
    }

    private static List<string> GetCSharpFiles(string path)
    {
        if (File.Exists(path) && path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
        {
            return new List<string> { path };
        }

        if (Directory.Exists(path))
        {
            return Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") &&
                           !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
                .ToList();
        }

        return new List<string>();
    }
}
