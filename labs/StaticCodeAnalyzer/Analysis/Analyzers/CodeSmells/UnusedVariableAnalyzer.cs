using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StaticCodeAnalyzer.Analysis.Analyzers.CodeSmells;

public class UnusedVariableAnalyzer : BaseCodeAnalyzer
{
    public override string AnalyzerId => "SMELL001";
    public override string Name => "Unused Variable Analyzer";
    public override IssueCategory Category => IssueCategory.CodeSmell;

    public override Task<IEnumerable<AnalysisResult>> AnalyzeAsync(
        SyntaxTree syntaxTree,
        SemanticModel? semanticModel,
        string filePath)
    {
        var results = new List<AnalysisResult>();
        var root = syntaxTree.GetRoot();

        // Check local variables
        var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();

        foreach (var method in methods)
        {
            var localDeclarations = method.DescendantNodes()
                .OfType<LocalDeclarationStatementSyntax>();

            foreach (var declaration in localDeclarations)
            {
                foreach (var variable in declaration.Declaration.Variables)
                {
                    var varName = variable.Identifier.Text;

                    // Skip discard variables
                    if (varName.StartsWith("_") && varName.Length == 1)
                        continue;

                    // Count usages (excluding the declaration itself)
                    var usages = method.DescendantNodes()
                        .OfType<IdentifierNameSyntax>()
                        .Where(id => id.Identifier.Text == varName &&
                                    id != variable.Identifier &&
                                    !IsPartOfDeclaration(id, variable));

                    if (!usages.Any())
                    {
                        results.Add(CreateResult(
                            "SMELL001",
                            "Unused Local Variable",
                            $"Variable '{varName}' is declared but never used.",
                            filePath,
                            variable.GetLocation(),
                            Severity.Minor,
                            GetCodeSnippet(declaration),
                            "Remove the unused variable or use discard '_' if intentionally ignoring a value."));
                    }
                }
            }

            // Check unused parameters
            foreach (var parameter in method.ParameterList.Parameters)
            {
                var paramName = parameter.Identifier.Text;

                // Skip discards and common patterns
                if (paramName.StartsWith("_"))
                    continue;

                var usages = method.Body?.DescendantNodes()
                    .OfType<IdentifierNameSyntax>()
                    .Where(id => id.Identifier.Text == paramName);

                var expressionUsages = method.ExpressionBody?.DescendantNodesAndSelf()
                    .OfType<IdentifierNameSyntax>()
                    .Where(id => id.Identifier.Text == paramName);

                bool isUsed = (usages?.Any() ?? false) || (expressionUsages?.Any() ?? false);

                // Check if method is virtual/override/interface implementation
                bool isOverridable = method.Modifiers.Any(m =>
                    m.IsKind(SyntaxKind.VirtualKeyword) ||
                    m.IsKind(SyntaxKind.OverrideKeyword) ||
                    m.IsKind(SyntaxKind.AbstractKeyword));

                // Check if method is event handler pattern
                bool isEventHandler = method.ParameterList.Parameters.Count == 2 &&
                    method.ParameterList.Parameters[0].Type?.ToString() == "object" &&
                    method.ParameterList.Parameters[1].Type?.ToString().EndsWith("EventArgs") == true;

                if (!isUsed && !isOverridable && !isEventHandler)
                {
                    results.Add(CreateResult(
                        "SMELL001",
                        "Unused Parameter",
                        $"Parameter '{paramName}' is not used in method '{method.Identifier.Text}'.",
                        filePath,
                        parameter.GetLocation(),
                        Severity.Minor,
                        $"{method.Identifier.Text}({paramName})",
                        "Remove the parameter or prefix with '_' to indicate intentional non-use."));
                }
            }
        }

        // Check unused private fields
        var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
        foreach (var classDecl in classes)
        {
            var privateFields = classDecl.Members
                .OfType<FieldDeclarationSyntax>()
                .Where(f => f.Modifiers.Any(m => m.IsKind(SyntaxKind.PrivateKeyword)) ||
                           !f.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword) ||
                                                 m.IsKind(SyntaxKind.ProtectedKeyword) ||
                                                 m.IsKind(SyntaxKind.InternalKeyword)));

            foreach (var field in privateFields)
            {
                foreach (var variable in field.Declaration.Variables)
                {
                    var fieldName = variable.Identifier.Text;

                    // Check for usages in the class
                    var usages = classDecl.DescendantNodes()
                        .OfType<IdentifierNameSyntax>()
                        .Where(id => id.Identifier.Text == fieldName &&
                                    !IsPartOfDeclaration(id, variable));

                    if (!usages.Any())
                    {
                        results.Add(CreateResult(
                            "SMELL001",
                            "Unused Private Field",
                            $"Private field '{fieldName}' is never used.",
                            filePath,
                            variable.GetLocation(),
                            Severity.Minor,
                            GetCodeSnippet(field),
                            "Remove the unused field."));
                    }
                }
            }
        }

        // Check for assigned but never read variables
        foreach (var method in methods)
        {
            var localDeclarations = method.DescendantNodes()
                .OfType<LocalDeclarationStatementSyntax>();

            foreach (var declaration in localDeclarations)
            {
                foreach (var variable in declaration.Declaration.Variables)
                {
                    var varName = variable.Identifier.Text;

                    if (varName.StartsWith("_"))
                        continue;

                    // Check if the variable is ever read (not just assigned)
                    var identifiers = method.DescendantNodes()
                        .OfType<IdentifierNameSyntax>()
                        .Where(id => id.Identifier.Text == varName);

                    bool isOnlyAssigned = identifiers.All(id =>
                        IsLeftSideOfAssignment(id) ||
                        IsPartOfDeclaration(id, variable));

                    if (identifiers.Any() && isOnlyAssigned && !identifiers.Any(IsInReturnOrCondition))
                    {
                        results.Add(CreateResult(
                            "SMELL001",
                            "Variable Assigned But Never Read",
                            $"Variable '{varName}' is assigned but its value is never read.",
                            filePath,
                            variable.GetLocation(),
                            Severity.Minor,
                            GetCodeSnippet(declaration),
                            "Remove the variable if the assigned value is not needed."));
                    }
                }
            }
        }

        return Task.FromResult<IEnumerable<AnalysisResult>>(results);
    }

    private static bool IsPartOfDeclaration(IdentifierNameSyntax id, VariableDeclaratorSyntax variable)
    {
        return id.Ancestors().Contains(variable);
    }

    private static bool IsLeftSideOfAssignment(IdentifierNameSyntax id)
    {
        var parent = id.Parent;

        if (parent is AssignmentExpressionSyntax assignment)
        {
            return assignment.Left == id;
        }

        if (parent is ArgumentSyntax argument)
        {
            return argument.RefOrOutKeyword.IsKind(SyntaxKind.OutKeyword);
        }

        return false;
    }

    private static bool IsInReturnOrCondition(IdentifierNameSyntax id)
    {
        return id.Ancestors().Any(a =>
            a is ReturnStatementSyntax ||
            a is IfStatementSyntax ||
            a is WhileStatementSyntax ||
            a is ConditionalExpressionSyntax);
    }
}
