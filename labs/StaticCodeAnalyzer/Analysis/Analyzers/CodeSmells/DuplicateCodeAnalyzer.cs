using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StaticCodeAnalyzer.Analysis.Analyzers.CodeSmells;

public class DuplicateCodeAnalyzer : BaseCodeAnalyzer
{
    public override string AnalyzerId => "SMELL004";
    public override string Name => "Duplicate Code Analyzer";
    public override IssueCategory Category => IssueCategory.CodeSmell;

    private const int MinDuplicateStatements = 5;
    private const int MinDuplicateLines = 6;

    public override Task<IEnumerable<AnalysisResult>> AnalyzeAsync(
        SyntaxTree syntaxTree,
        SemanticModel? semanticModel,
        string filePath)
    {
        var results = new List<AnalysisResult>();
        var root = syntaxTree.GetRoot();

        // Find duplicate method bodies
        var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Where(m => m.Body != null)
            .ToList();

        var reportedDuplicates = new HashSet<string>();

        for (int i = 0; i < methods.Count; i++)
        {
            for (int j = i + 1; j < methods.Count; j++)
            {
                var method1 = methods[i];
                var method2 = methods[j];

                var normalizedBody1 = NormalizeCode(method1.Body!.ToString());
                var normalizedBody2 = NormalizeCode(method2.Body!.ToString());

                // Check for exact duplicates (after normalization)
                if (normalizedBody1 == normalizedBody2 &&
                    normalizedBody1.Length > 100) // Skip trivial methods
                {
                    var key = string.Join("_", new[] { method1.Identifier.Text, method2.Identifier.Text }.OrderBy(x => x));

                    if (!reportedDuplicates.Contains(key))
                    {
                        reportedDuplicates.Add(key);

                        results.Add(CreateResult(
                            "SMELL004",
                            "Duplicate Method Bodies",
                            $"Methods '{method1.Identifier.Text}' and '{method2.Identifier.Text}' have identical bodies.",
                            filePath,
                            method1.GetLocation(),
                            Severity.Major,
                            $"{method1.Identifier.Text}() and {method2.Identifier.Text}()",
                            "Extract common logic to a shared method."));
                    }
                }
            }
        }

        // Find duplicate code blocks within the same file
        var blocks = root.DescendantNodes().OfType<BlockSyntax>()
            .Where(b => b.Statements.Count >= MinDuplicateStatements)
            .ToList();

        for (int i = 0; i < blocks.Count; i++)
        {
            for (int j = i + 1; j < blocks.Count; j++)
            {
                var similarity = CalculateSimilarity(blocks[i], blocks[j]);

                if (similarity > 0.9) // 90% similar
                {
                    var lineCount1 = CountLines(blocks[i]);
                    var lineCount2 = CountLines(blocks[j]);

                    if (lineCount1 >= MinDuplicateLines && lineCount2 >= MinDuplicateLines)
                    {
                        results.Add(CreateResult(
                            "SMELL004",
                            "Duplicate Code Block",
                            $"Similar code blocks found ({(int)(similarity * 100)}% similarity). Lines: {lineCount1} and {lineCount2}.",
                            filePath,
                            blocks[i].GetLocation(),
                            Severity.Major,
                            GetCodeSnippet(blocks[i]),
                            "Consider extracting the common logic into a reusable method."));

                        // Don't report the second block to avoid noise
                        break;
                    }
                }
            }
        }

        // Find copy-paste patterns (similar consecutive statements)
        foreach (var method in methods.Where(m => m.Body != null))
        {
            var statements = method.Body!.Statements.ToList();

            for (int i = 0; i < statements.Count - MinDuplicateStatements; i++)
            {
                for (int j = i + MinDuplicateStatements; j < statements.Count; j++)
                {
                    int matchLength = 0;
                    int k = 0;

                    while (i + k < j && j + k < statements.Count)
                    {
                        var stmt1 = NormalizeCode(statements[i + k].ToString());
                        var stmt2 = NormalizeCode(statements[j + k].ToString());

                        if (AreSimilarStatements(stmt1, stmt2))
                        {
                            matchLength++;
                            k++;
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (matchLength >= MinDuplicateStatements)
                    {
                        results.Add(CreateResult(
                            "SMELL004",
                            "Repeated Code Pattern",
                            $"Found {matchLength} similar consecutive statements repeated in method '{method.Identifier.Text}'.",
                            filePath,
                            statements[i].GetLocation(),
                            Severity.Minor,
                            GetCodeSnippet(statements[i]),
                            "Consider refactoring using loops or extracting to a method."));

                        // Skip to avoid multiple reports for the same pattern
                        break;
                    }
                }
            }
        }

        // Check for duplicate catch blocks
        var tryStatements = root.DescendantNodes().OfType<TryStatementSyntax>();
        foreach (var tryStmt in tryStatements)
        {
            var catches = tryStmt.Catches.ToList();

            for (int i = 0; i < catches.Count; i++)
            {
                for (int j = i + 1; j < catches.Count; j++)
                {
                    var body1 = NormalizeCode(catches[i].Block.ToString());
                    var body2 = NormalizeCode(catches[j].Block.ToString());

                    if (body1 == body2 && body1.Length > 20)
                    {
                        results.Add(CreateResult(
                            "SMELL004",
                            "Duplicate Catch Blocks",
                            "Multiple catch blocks have identical handling code.",
                            filePath,
                            catches[i].GetLocation(),
                            Severity.Minor,
                            GetCodeSnippet(catches[i]),
                            "Consider using a single catch with exception filters or a when clause."));
                        break;
                    }
                }
            }
        }

        return Task.FromResult<IEnumerable<AnalysisResult>>(results);
    }

    private static string NormalizeCode(string code)
    {
        // Remove whitespace and normalize for comparison
        return string.Join("",
            code.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            .ToLowerInvariant();
    }

    private static double CalculateSimilarity(BlockSyntax block1, BlockSyntax block2)
    {
        var statements1 = block1.Statements.Select(s => NormalizeCode(s.ToString())).ToList();
        var statements2 = block2.Statements.Select(s => NormalizeCode(s.ToString())).ToList();

        if (statements1.Count == 0 || statements2.Count == 0)
            return 0;

        int matches = 0;
        foreach (var stmt1 in statements1)
        {
            if (statements2.Any(stmt2 => AreSimilarStatements(stmt1, stmt2)))
            {
                matches++;
            }
        }

        return (double)matches / Math.Max(statements1.Count, statements2.Count);
    }

    private static bool AreSimilarStatements(string stmt1, string stmt2)
    {
        if (stmt1 == stmt2)
            return true;

        // Allow for variable name differences
        // This is a simplified check; real implementation would use symbol analysis
        if (Math.Abs(stmt1.Length - stmt2.Length) <= stmt1.Length * 0.1)
        {
            // Check structure similarity by comparing non-identifier parts
            var normalized1 = NormalizeIdentifiers(stmt1);
            var normalized2 = NormalizeIdentifiers(stmt2);
            return normalized1 == normalized2;
        }

        return false;
    }

    private static string NormalizeIdentifiers(string code)
    {
        // Very simplified - replace potential identifiers with placeholders
        // A real implementation would use semantic analysis
        var result = code;

        // Replace common patterns
        for (char c = 'a'; c <= 'z'; c++)
        {
            result = result.Replace(c.ToString(), "_");
        }

        return result;
    }

    private static int CountLines(BlockSyntax block)
    {
        var span = block.GetLocation().GetLineSpan();
        return span.EndLinePosition.Line - span.StartLinePosition.Line + 1;
    }
}
