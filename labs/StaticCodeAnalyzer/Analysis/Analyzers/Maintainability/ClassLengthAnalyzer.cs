using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StaticCodeAnalyzer.Analysis.Analyzers.Maintainability;

public class ClassLengthAnalyzer : BaseCodeAnalyzer
{
    public override string AnalyzerId => "MAINT003";
    public override string Name => "Class Length Analyzer";
    public override IssueCategory Category => IssueCategory.Maintainability;

    private const int LinesWarningThreshold = 300;
    private const int LinesCriticalThreshold = 500;
    private const int MethodsWarningThreshold = 20;
    private const int MethodsCriticalThreshold = 30;
    private const int FieldsWarningThreshold = 15;

    public override Task<IEnumerable<AnalysisResult>> AnalyzeAsync(
        SyntaxTree syntaxTree,
        SemanticModel? semanticModel,
        string filePath)
    {
        var results = new List<AnalysisResult>();
        var root = syntaxTree.GetRoot();

        var typeDeclarations = root.DescendantNodes()
            .Where(n => n is ClassDeclarationSyntax || n is StructDeclarationSyntax || n is RecordDeclarationSyntax);

        foreach (var typeDecl in typeDeclarations)
        {
            string typeName;
            Location location;

            switch (typeDecl)
            {
                case ClassDeclarationSyntax classDecl:
                    typeName = classDecl.Identifier.Text;
                    location = classDecl.Identifier.GetLocation();
                    break;
                case StructDeclarationSyntax structDecl:
                    typeName = structDecl.Identifier.Text;
                    location = structDecl.Identifier.GetLocation();
                    break;
                case RecordDeclarationSyntax recordDecl:
                    typeName = recordDecl.Identifier.Text;
                    location = recordDecl.Identifier.GetLocation();
                    break;
                default:
                    continue;
            }

            // Check line count
            var lineSpan = typeDecl.GetLocation().GetLineSpan();
            int lineCount = lineSpan.EndLinePosition.Line - lineSpan.StartLinePosition.Line + 1;

            if (lineCount >= LinesCriticalThreshold)
            {
                results.Add(CreateResult(
                    "MAINT003",
                    "Very Large Class",
                    $"Class '{typeName}' has {lineCount} lines. Consider splitting into smaller classes.",
                    filePath,
                    location,
                    Severity.Critical,
                    $"{typeName} - {lineCount} lines",
                    "Apply Single Responsibility Principle. Extract related functionality into separate classes."));
            }
            else if (lineCount >= LinesWarningThreshold)
            {
                results.Add(CreateResult(
                    "MAINT003",
                    "Large Class",
                    $"Class '{typeName}' has {lineCount} lines. Maximum recommended is {LinesWarningThreshold}.",
                    filePath,
                    location,
                    Severity.Major,
                    $"{typeName} - {lineCount} lines",
                    "Consider whether this class has too many responsibilities."));
            }

            // Check method count
            var methodCount = typeDecl.DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .Count(m => m.Parent == typeDecl ||
                           (m.Parent is GlobalStatementSyntax gs && gs.Parent == typeDecl));

            // Include direct child methods only (not nested class methods)
            methodCount = typeDecl.ChildNodes().OfType<MethodDeclarationSyntax>().Count();
            if (typeDecl is TypeDeclarationSyntax tds)
            {
                methodCount = tds.Members.OfType<MethodDeclarationSyntax>().Count();
            }

            if (methodCount >= MethodsCriticalThreshold)
            {
                results.Add(CreateResult(
                    "MAINT003",
                    "Too Many Methods",
                    $"Class '{typeName}' has {methodCount} methods. Consider splitting the class.",
                    filePath,
                    location,
                    Severity.Critical,
                    $"{typeName} - {methodCount} methods",
                    "Classes with too many methods often violate Single Responsibility Principle."));
            }
            else if (methodCount >= MethodsWarningThreshold)
            {
                results.Add(CreateResult(
                    "MAINT003",
                    "Many Methods",
                    $"Class '{typeName}' has {methodCount} methods. Maximum recommended is {MethodsWarningThreshold}.",
                    filePath,
                    location,
                    Severity.Major,
                    $"{typeName} - {methodCount} methods",
                    "Consider grouping related methods into separate classes."));
            }

            // Check field count
            int fieldCount = 0;
            if (typeDecl is TypeDeclarationSyntax typeDeclaration)
            {
                fieldCount = typeDeclaration.Members.OfType<FieldDeclarationSyntax>()
                    .SelectMany(f => f.Declaration.Variables)
                    .Count();
            }

            if (fieldCount >= FieldsWarningThreshold)
            {
                results.Add(CreateResult(
                    "MAINT003",
                    "Too Many Fields",
                    $"Class '{typeName}' has {fieldCount} fields. This may indicate too many responsibilities.",
                    filePath,
                    location,
                    Severity.Major,
                    $"{typeName} - {fieldCount} fields",
                    "Consider grouping related fields into separate classes or structs."));
            }

            // Check for deeply nested types
            var nestedTypes = typeDecl.DescendantNodes()
                .Where(n => n is ClassDeclarationSyntax || n is StructDeclarationSyntax);

            foreach (var nested in nestedTypes)
            {
                var nestedDepth = nested.Ancestors()
                    .Count(a => a is ClassDeclarationSyntax || a is StructDeclarationSyntax);

                if (nestedDepth > 2)
                {
                    var nestedName = nested switch
                    {
                        ClassDeclarationSyntax c => c.Identifier.Text,
                        StructDeclarationSyntax s => s.Identifier.Text,
                        _ => "Unknown"
                    };

                    results.Add(CreateResult(
                        "MAINT003",
                        "Deeply Nested Type",
                        $"Type '{nestedName}' is nested {nestedDepth} levels deep.",
                        filePath,
                        nested.GetLocation(),
                        Severity.Minor,
                        $"Nesting depth: {nestedDepth}",
                        "Consider moving deeply nested types to their own files."));
                }
            }
        }

        return Task.FromResult<IEnumerable<AnalysisResult>>(results);
    }
}
