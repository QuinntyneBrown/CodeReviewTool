using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StaticCodeAnalyzer.Analysis.Analyzers.CodeSmells;

public class EmptyBlockAnalyzer : BaseCodeAnalyzer
{
    public override string AnalyzerId => "SMELL003";
    public override string Name => "Empty Block Analyzer";
    public override IssueCategory Category => IssueCategory.CodeSmell;

    public override Task<IEnumerable<AnalysisResult>> AnalyzeAsync(
        SyntaxTree syntaxTree,
        SemanticModel? semanticModel,
        string filePath)
    {
        var results = new List<AnalysisResult>();
        var root = syntaxTree.GetRoot();

        // Check empty if blocks
        var ifStatements = root.DescendantNodes().OfType<IfStatementSyntax>();
        foreach (var ifStmt in ifStatements)
        {
            if (IsEmptyBlock(ifStmt.Statement))
            {
                results.Add(CreateResult(
                    "SMELL003",
                    "Empty If Block",
                    "Empty if block found. Either add implementation or remove the condition.",
                    filePath,
                    ifStmt.GetLocation(),
                    Severity.Minor,
                    GetCodeSnippet(ifStmt),
                    "Add implementation, invert the condition, or remove the if statement."));
            }

            // Check empty else blocks
            if (ifStmt.Else != null && IsEmptyBlock(ifStmt.Else.Statement))
            {
                results.Add(CreateResult(
                    "SMELL003",
                    "Empty Else Block",
                    "Empty else block found.",
                    filePath,
                    ifStmt.Else.GetLocation(),
                    Severity.Minor,
                    GetCodeSnippet(ifStmt.Else),
                    "Add implementation or remove the else clause."));
            }
        }

        // Check empty loops
        var forStatements = root.DescendantNodes().OfType<ForStatementSyntax>();
        foreach (var forStmt in forStatements)
        {
            if (IsEmptyBlock(forStmt.Statement))
            {
                // Check if it's a spin-wait pattern (might be intentional)
                if (!IsSpinWaitPattern(forStmt))
                {
                    results.Add(CreateResult(
                        "SMELL003",
                        "Empty For Loop",
                        "Empty for loop body found. This may indicate missing implementation.",
                        filePath,
                        forStmt.GetLocation(),
                        Severity.Major,
                        GetCodeSnippet(forStmt),
                        "Add loop body implementation or remove if unneeded."));
                }
            }
        }

        var whileStatements = root.DescendantNodes().OfType<WhileStatementSyntax>();
        foreach (var whileStmt in whileStatements)
        {
            if (IsEmptyBlock(whileStmt.Statement))
            {
                results.Add(CreateResult(
                    "SMELL003",
                    "Empty While Loop",
                    "Empty while loop body found. This may indicate missing implementation or busy-wait.",
                    filePath,
                    whileStmt.GetLocation(),
                    Severity.Major,
                    GetCodeSnippet(whileStmt),
                    "Add loop body or use proper synchronization primitives."));
            }
        }

        var foreachStatements = root.DescendantNodes().OfType<ForEachStatementSyntax>();
        foreach (var foreachStmt in foreachStatements)
        {
            if (IsEmptyBlock(foreachStmt.Statement))
            {
                results.Add(CreateResult(
                    "SMELL003",
                    "Empty Foreach Loop",
                    "Empty foreach loop body found.",
                    filePath,
                    foreachStmt.GetLocation(),
                    Severity.Major,
                    GetCodeSnippet(foreachStmt),
                    "Add loop body or use LINQ .Any() / .Count() if just checking enumeration."));
            }
        }

        // Check empty try blocks
        var tryStatements = root.DescendantNodes().OfType<TryStatementSyntax>();
        foreach (var tryStmt in tryStatements)
        {
            if (IsEmptyBlock(tryStmt.Block))
            {
                results.Add(CreateResult(
                    "SMELL003",
                    "Empty Try Block",
                    "Empty try block found. This is likely incomplete code.",
                    filePath,
                    tryStmt.GetLocation(),
                    Severity.Major,
                    GetCodeSnippet(tryStmt),
                    "Add implementation to the try block or remove the try-catch."));
            }
        }

        // Check empty methods
        var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();
        foreach (var method in methods)
        {
            if (method.Body != null && IsEmptyBlock(method.Body))
            {
                // Skip abstract, interface implementations, virtual methods (might be intentional)
                if (method.Modifiers.Any(m =>
                    m.IsKind(SyntaxKind.AbstractKeyword) ||
                    m.IsKind(SyntaxKind.VirtualKeyword) ||
                    m.IsKind(SyntaxKind.OverrideKeyword)))
                {
                    continue;
                }

                // Skip partial methods
                if (method.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
                    continue;

                results.Add(CreateResult(
                    "SMELL003",
                    "Empty Method",
                    $"Method '{method.Identifier.Text}' has an empty body.",
                    filePath,
                    method.Identifier.GetLocation(),
                    Severity.Minor,
                    $"{method.Identifier.Text}() {{ }}",
                    "Add implementation, throw NotImplementedException, or document why it's intentionally empty."));
            }
        }

        // Check empty constructors
        var constructors = root.DescendantNodes().OfType<ConstructorDeclarationSyntax>();
        foreach (var ctor in constructors)
        {
            if (ctor.Body != null && IsEmptyBlock(ctor.Body) && ctor.Initializer == null)
            {
                // Check if class has other constructors (this one might be for specific scenarios)
                var classDecl = ctor.Parent as ClassDeclarationSyntax;
                if (classDecl != null)
                {
                    var otherCtors = classDecl.Members
                        .OfType<ConstructorDeclarationSyntax>()
                        .Count(c => c != ctor);

                    if (otherCtors == 0)
                    {
                        results.Add(CreateResult(
                            "SMELL003",
                            "Empty Constructor",
                            $"Constructor for '{ctor.Identifier.Text}' is empty and can be removed.",
                            filePath,
                            ctor.Identifier.GetLocation(),
                            Severity.Info,
                            $"{ctor.Identifier.Text}() {{ }}",
                            "Remove empty default constructor if it's unnecessary."));
                    }
                }
            }
        }

        // Check empty finally blocks
        foreach (var tryStmt in tryStatements)
        {
            if (tryStmt.Finally != null && IsEmptyBlock(tryStmt.Finally.Block))
            {
                results.Add(CreateResult(
                    "SMELL003",
                    "Empty Finally Block",
                    "Empty finally block found. Either add cleanup code or remove the finally clause.",
                    filePath,
                    tryStmt.Finally.GetLocation(),
                    Severity.Minor,
                    GetCodeSnippet(tryStmt.Finally),
                    "Add cleanup code or remove the finally block."));
            }
        }

        return Task.FromResult<IEnumerable<AnalysisResult>>(results);
    }

    private static bool IsEmptyBlock(StatementSyntax statement)
    {
        if (statement is BlockSyntax block)
        {
            return block.Statements.Count == 0;
        }

        if (statement is EmptyStatementSyntax)
        {
            return true;
        }

        return false;
    }

    private static bool IsSpinWaitPattern(ForStatementSyntax forStmt)
    {
        // Check for patterns like: for(int i = 0; i < count; i++) ;
        // This might be an intentional delay, though not recommended
        var condition = forStmt.Condition?.ToString() ?? "";
        return condition.Contains("SpinWait") ||
               forStmt.Statement is EmptyStatementSyntax;
    }
}
