using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StaticCodeAnalyzer.Analysis.Analyzers.Reliability;

public class InfiniteLoopAnalyzer : BaseCodeAnalyzer
{
    public override string AnalyzerId => "REL006";
    public override string Name => "Infinite Loop Analyzer";
    public override IssueCategory Category => IssueCategory.Reliability;

    public override Task<IEnumerable<AnalysisResult>> AnalyzeAsync(
        SyntaxTree syntaxTree,
        SemanticModel? semanticModel,
        string filePath)
    {
        var results = new List<AnalysisResult>();
        var root = syntaxTree.GetRoot();

        // Check for while(true) without break/return
        var whileStatements = root.DescendantNodes().OfType<WhileStatementSyntax>();
        foreach (var whileStmt in whileStatements)
        {
            if (IsAlwaysTrue(whileStmt.Condition))
            {
                if (!HasExitPath(whileStmt.Statement))
                {
                    results.Add(CreateResult(
                        "REL006",
                        "Potential Infinite Loop",
                        "while(true) loop without visible break, return, or throw statement.",
                        filePath,
                        whileStmt.GetLocation(),
                        Severity.Critical,
                        GetCodeSnippet(whileStmt),
                        "Ensure the loop has a valid exit condition or break statement.",
                        "CWE-835"));
                }
            }

            // Check for condition that never changes
            if (IsLoopConditionConstant(whileStmt))
            {
                results.Add(CreateResult(
                    "REL006",
                    "Loop Condition May Not Change",
                    "Loop condition variable may not be modified inside the loop.",
                    filePath,
                    whileStmt.GetLocation(),
                    Severity.Major,
                    GetCodeSnippet(whileStmt),
                    "Ensure the loop condition variable is modified inside the loop.",
                    "CWE-835"));
            }
        }

        // Check for for(;;) without break/return
        var forStatements = root.DescendantNodes().OfType<ForStatementSyntax>();
        foreach (var forStmt in forStatements)
        {
            if (forStmt.Condition == null && !HasExitPath(forStmt.Statement))
            {
                results.Add(CreateResult(
                    "REL006",
                    "Potential Infinite Loop",
                    "for(;;) loop without visible break, return, or throw statement.",
                    filePath,
                    forStmt.GetLocation(),
                    Severity.Critical,
                    GetCodeSnippet(forStmt),
                    "Ensure the loop has a valid exit condition.",
                    "CWE-835"));
            }

            // Check for loop where incrementor doesn't affect condition
            if (forStmt.Condition != null && forStmt.Incrementors.Any())
            {
                var conditionVars = GetVariablesInExpression(forStmt.Condition);
                var incrementorVars = forStmt.Incrementors
                    .SelectMany(GetModifiedVariables);

                if (!conditionVars.Intersect(incrementorVars).Any())
                {
                    results.Add(CreateResult(
                        "REL006",
                        "Loop Incrementor May Not Affect Condition",
                        "Loop incrementor does not appear to modify variables in the condition.",
                        filePath,
                        forStmt.GetLocation(),
                        Severity.Major,
                        GetCodeSnippet(forStmt),
                        "Ensure the loop incrementor affects the loop condition.",
                        "CWE-835"));
                }
            }
        }

        // Check for do-while with constant true
        var doStatements = root.DescendantNodes().OfType<DoStatementSyntax>();
        foreach (var doStmt in doStatements)
        {
            if (IsAlwaysTrue(doStmt.Condition) && !HasExitPath(doStmt.Statement))
            {
                results.Add(CreateResult(
                    "REL006",
                    "Potential Infinite Loop",
                    "do-while(true) loop without visible break, return, or throw statement.",
                    filePath,
                    doStmt.GetLocation(),
                    Severity.Critical,
                    GetCodeSnippet(doStmt),
                    "Ensure the loop has a valid exit condition.",
                    "CWE-835"));
            }
        }

        // Check for foreach on self-modifying collection
        var foreachStatements = root.DescendantNodes().OfType<ForEachStatementSyntax>();
        foreach (var foreachStmt in foreachStatements)
        {
            var collectionName = foreachStmt.Expression.ToString();

            // Check if collection is modified inside loop
            var modifyingMethods = new[] { "Add", "Remove", "Insert", "Clear", "RemoveAt" };
            var invocations = foreachStmt.Statement.DescendantNodes().OfType<InvocationExpressionSyntax>();

            foreach (var invocation in invocations)
            {
                var methodText = invocation.Expression.ToString();
                if (methodText.StartsWith(collectionName) &&
                    modifyingMethods.Any(m => methodText.Contains($".{m}")))
                {
                    results.Add(CreateResult(
                        "REL006",
                        "Collection Modified During Enumeration",
                        $"Collection '{collectionName}' appears to be modified while being enumerated.",
                        filePath,
                        invocation.GetLocation(),
                        Severity.Critical,
                        GetCodeSnippet(invocation),
                        "Use a separate list for modifications or iterate over a copy.",
                        "CWE-1335"));
                }
            }
        }

        // Check for recursive calls without base case
        var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();
        foreach (var method in methods)
        {
            var methodName = method.Identifier.Text;
            var recursiveCalls = method.DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .Where(i => GetMethodName(i) == methodName);

            foreach (var call in recursiveCalls)
            {
                // Check if all paths to recursive call have a base case
                var containingIf = call.Ancestors().OfType<IfStatementSyntax>().FirstOrDefault();
                if (containingIf == null)
                {
                    // Recursive call not inside an if statement - might be intentional but risky
                    results.Add(CreateResult(
                        "REL006",
                        "Unconditional Recursive Call",
                        $"Method '{methodName}' contains unconditional recursive call. This may cause stack overflow.",
                        filePath,
                        call.GetLocation(),
                        Severity.Critical,
                        GetCodeSnippet(call),
                        "Ensure recursive calls have a proper base case condition.",
                        "CWE-674"));
                }
            }
        }

        return Task.FromResult<IEnumerable<AnalysisResult>>(results);
    }

    private static bool IsAlwaysTrue(ExpressionSyntax condition)
    {
        if (condition is LiteralExpressionSyntax literal)
        {
            return literal.IsKind(SyntaxKind.TrueLiteralExpression);
        }
        return false;
    }

    private static bool HasExitPath(StatementSyntax statement)
    {
        return statement.DescendantNodes().Any(n =>
            n is BreakStatementSyntax ||
            n is ReturnStatementSyntax ||
            n is ThrowStatementSyntax ||
            (n is InvocationExpressionSyntax inv &&
             inv.Expression.ToString().Contains("Environment.Exit")));
    }

    private static bool IsLoopConditionConstant(WhileStatementSyntax whileStmt)
    {
        var condition = whileStmt.Condition;
        var variables = GetVariablesInExpression(condition);

        if (!variables.Any())
            return false;

        var body = whileStmt.Statement;
        var modifiedVars = GetModifiedVariablesInBlock(body);

        return !variables.Intersect(modifiedVars).Any();
    }

    private static HashSet<string> GetVariablesInExpression(ExpressionSyntax expression)
    {
        var variables = new HashSet<string>();

        foreach (var identifier in expression.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>())
        {
            variables.Add(identifier.Identifier.Text);
        }

        return variables;
    }

    private static HashSet<string> GetModifiedVariablesInBlock(StatementSyntax statement)
    {
        var modified = new HashSet<string>();

        // Assignments
        foreach (var assignment in statement.DescendantNodes().OfType<AssignmentExpressionSyntax>())
        {
            if (assignment.Left is IdentifierNameSyntax identifier)
            {
                modified.Add(identifier.Identifier.Text);
            }
        }

        // Prefix/postfix increments
        foreach (var prefix in statement.DescendantNodes().OfType<PrefixUnaryExpressionSyntax>())
        {
            if ((prefix.IsKind(SyntaxKind.PreIncrementExpression) ||
                 prefix.IsKind(SyntaxKind.PreDecrementExpression)) &&
                prefix.Operand is IdentifierNameSyntax id)
            {
                modified.Add(id.Identifier.Text);
            }
        }

        foreach (var postfix in statement.DescendantNodes().OfType<PostfixUnaryExpressionSyntax>())
        {
            if ((postfix.IsKind(SyntaxKind.PostIncrementExpression) ||
                 postfix.IsKind(SyntaxKind.PostDecrementExpression)) &&
                postfix.Operand is IdentifierNameSyntax id)
            {
                modified.Add(id.Identifier.Text);
            }
        }

        return modified;
    }

    private static IEnumerable<string> GetModifiedVariables(ExpressionSyntax expression)
    {
        if (expression is AssignmentExpressionSyntax assignment)
        {
            if (assignment.Left is IdentifierNameSyntax id)
                yield return id.Identifier.Text;
        }
        else if (expression is PrefixUnaryExpressionSyntax prefix &&
                 prefix.Operand is IdentifierNameSyntax prefixId)
        {
            yield return prefixId.Identifier.Text;
        }
        else if (expression is PostfixUnaryExpressionSyntax postfix &&
                 postfix.Operand is IdentifierNameSyntax postfixId)
        {
            yield return postfixId.Identifier.Text;
        }
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
}
