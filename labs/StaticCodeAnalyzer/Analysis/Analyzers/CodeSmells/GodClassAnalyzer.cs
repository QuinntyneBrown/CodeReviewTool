using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StaticCodeAnalyzer.Analysis.Analyzers.CodeSmells;

public class GodClassAnalyzer : BaseCodeAnalyzer
{
    public override string AnalyzerId => "SMELL006";
    public override string Name => "God Class Analyzer";
    public override IssueCategory Category => IssueCategory.CodeSmell;

    // Thresholds for God Class detection
    private const int MaxPublicMethods = 15;
    private const int MaxDependencies = 10;
    private const int MaxResponsibilities = 5;

    public override Task<IEnumerable<AnalysisResult>> AnalyzeAsync(
        SyntaxTree syntaxTree,
        SemanticModel? semanticModel,
        string filePath)
    {
        var results = new List<AnalysisResult>();
        var root = syntaxTree.GetRoot();

        var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();

        foreach (var classDecl in classes)
        {
            var className = classDecl.Identifier.Text;

            // Count public methods
            var publicMethods = classDecl.Members
                .OfType<MethodDeclarationSyntax>()
                .Count(m => m.Modifiers.Any(mod => mod.IsKind(SyntaxKind.PublicKeyword)));

            // Count dependencies (fields/properties that are injected or created)
            var dependencies = CountDependencies(classDecl);

            // Estimate responsibilities based on method naming patterns
            var responsibilities = EstimateResponsibilities(classDecl);

            // Calculate "god class" score
            bool isGodClass = false;
            var issues = new List<string>();

            if (publicMethods > MaxPublicMethods)
            {
                issues.Add($"{publicMethods} public methods (max: {MaxPublicMethods})");
                isGodClass = true;
            }

            if (dependencies > MaxDependencies)
            {
                issues.Add($"{dependencies} dependencies (max: {MaxDependencies})");
                isGodClass = true;
            }

            if (responsibilities > MaxResponsibilities)
            {
                issues.Add($"~{responsibilities} different responsibilities (max: {MaxResponsibilities})");
                isGodClass = true;
            }

            if (isGodClass)
            {
                var severity = issues.Count >= 3 ? Severity.Critical :
                              issues.Count >= 2 ? Severity.Major : Severity.Minor;

                results.Add(CreateResult(
                    "SMELL006",
                    "Potential God Class",
                    $"Class '{className}' may be a God Class: {string.Join("; ", issues)}.",
                    filePath,
                    classDecl.Identifier.GetLocation(),
                    severity,
                    className,
                    "Apply Single Responsibility Principle. Split class into focused classes with clear responsibilities."));
            }

            // Check for feature envy (methods that use other classes more than their own)
            var methods = classDecl.Members.OfType<MethodDeclarationSyntax>();
            foreach (var method in methods)
            {
                var methodName = method.Identifier.Text;
                var (ownUsages, externalUsages) = CountFieldUsages(method, classDecl);

                if (externalUsages > 0 && externalUsages > ownUsages * 2 && externalUsages >= 5)
                {
                    results.Add(CreateResult(
                        "SMELL006",
                        "Feature Envy",
                        $"Method '{methodName}' uses external class members ({externalUsages}) more than its own ({ownUsages}).",
                        filePath,
                        method.Identifier.GetLocation(),
                        Severity.Minor,
                        methodName,
                        "Consider moving this method to the class whose data it uses most."));
                }
            }

            // Check for data class (class with only properties and no behavior)
            var methodsWithLogic = classDecl.Members
                .OfType<MethodDeclarationSyntax>()
                .Where(m => m.Body?.Statements.Count > 1 ||
                           m.ExpressionBody != null)
                .Count();

            var properties = classDecl.Members.OfType<PropertyDeclarationSyntax>().Count();

            if (properties > 5 && methodsWithLogic == 0)
            {
                results.Add(CreateResult(
                    "SMELL006",
                    "Data Class",
                    $"Class '{className}' has {properties} properties but no behavior (methods with logic).",
                    filePath,
                    classDecl.Identifier.GetLocation(),
                    Severity.Info,
                    className,
                    "Consider if behavior related to this data should be in this class (OOP) or if this is intentionally a DTO/record."));
            }

            // Check for inappropriate intimacy (excessive access to other class internals)
            var otherClassAccesses = CountOtherClassPropertyAccesses(classDecl);
            if (otherClassAccesses.Any(kv => kv.Value > 10))
            {
                var mostAccessed = otherClassAccesses.OrderByDescending(kv => kv.Value).First();
                results.Add(CreateResult(
                    "SMELL006",
                    "Inappropriate Intimacy",
                    $"Class '{className}' accesses '{mostAccessed.Key}' members {mostAccessed.Value} times.",
                    filePath,
                    classDecl.Identifier.GetLocation(),
                    Severity.Minor,
                    $"{className} -> {mostAccessed.Key}",
                    "Consider if these classes should be merged or if behavior should be moved."));
            }
        }

        return Task.FromResult<IEnumerable<AnalysisResult>>(results);
    }

    private static int CountDependencies(ClassDeclarationSyntax classDecl)
    {
        // Count field and property types that look like dependencies
        var fieldTypes = classDecl.Members
            .OfType<FieldDeclarationSyntax>()
            .Select(f => f.Declaration.Type.ToString())
            .Where(t => !IsPrimitiveType(t));

        var propertyTypes = classDecl.Members
            .OfType<PropertyDeclarationSyntax>()
            .Select(p => p.Type.ToString())
            .Where(t => !IsPrimitiveType(t));

        // Also count constructor parameters
        var ctorParams = classDecl.Members
            .OfType<ConstructorDeclarationSyntax>()
            .SelectMany(c => c.ParameterList.Parameters)
            .Select(p => p.Type?.ToString() ?? "")
            .Where(t => !IsPrimitiveType(t));

        return fieldTypes.Concat(propertyTypes).Concat(ctorParams)
            .Distinct()
            .Count();
    }

    private static bool IsPrimitiveType(string typeName)
    {
        var primitives = new[]
        {
            "int", "string", "bool", "double", "float", "decimal", "long", "short",
            "byte", "char", "object", "dynamic", "void", "DateTime", "Guid", "TimeSpan"
        };

        return primitives.Any(p =>
            typeName.Equals(p, StringComparison.OrdinalIgnoreCase) ||
            typeName.StartsWith($"{p}?") ||
            typeName.StartsWith($"System.{p}"));
    }

    private static int EstimateResponsibilities(ClassDeclarationSyntax classDecl)
    {
        // Group methods by naming prefixes to estimate responsibilities
        var methods = classDecl.Members
            .OfType<MethodDeclarationSyntax>()
            .Select(m => m.Identifier.Text)
            .ToList();

        var prefixes = new HashSet<string>();

        foreach (var method in methods)
        {
            // Extract action prefix (Get, Set, Create, Update, Delete, Handle, Process, etc.)
            var prefix = ExtractActionPrefix(method);
            if (!string.IsNullOrEmpty(prefix))
            {
                prefixes.Add(prefix);
            }
        }

        return Math.Max(1, prefixes.Count);
    }

    private static string ExtractActionPrefix(string methodName)
    {
        var commonPrefixes = new[]
        {
            "Get", "Set", "Create", "Update", "Delete", "Remove", "Add",
            "Handle", "Process", "Validate", "Calculate", "Generate", "Build",
            "Load", "Save", "Read", "Write", "Send", "Receive", "Parse", "Format"
        };

        foreach (var prefix in commonPrefixes)
        {
            if (methodName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return prefix;
            }
        }

        return "";
    }

    private static (int ownUsages, int externalUsages) CountFieldUsages(
        MethodDeclarationSyntax method,
        ClassDeclarationSyntax classDecl)
    {
        var classFields = classDecl.Members
            .OfType<FieldDeclarationSyntax>()
            .SelectMany(f => f.Declaration.Variables)
            .Select(v => v.Identifier.Text)
            .ToHashSet();

        var classProperties = classDecl.Members
            .OfType<PropertyDeclarationSyntax>()
            .Select(p => p.Identifier.Text)
            .ToHashSet();

        var ownMembers = classFields.Union(classProperties).ToHashSet();

        int ownUsages = 0;
        int externalUsages = 0;

        var memberAccesses = method.DescendantNodes().OfType<MemberAccessExpressionSyntax>();

        foreach (var access in memberAccesses)
        {
            var memberName = access.Name.Identifier.Text;

            if (access.Expression is ThisExpressionSyntax ||
                ownMembers.Contains(memberName))
            {
                ownUsages++;
            }
            else if (access.Expression is IdentifierNameSyntax)
            {
                externalUsages++;
            }
        }

        return (ownUsages, externalUsages);
    }

    private static Dictionary<string, int> CountOtherClassPropertyAccesses(ClassDeclarationSyntax classDecl)
    {
        var result = new Dictionary<string, int>();

        var memberAccesses = classDecl.DescendantNodes().OfType<MemberAccessExpressionSyntax>();

        foreach (var access in memberAccesses)
        {
            if (access.Expression is IdentifierNameSyntax identifier)
            {
                var typeName = identifier.Identifier.Text;

                // Skip common patterns
                if (typeName == "this" || typeName == "base" ||
                    char.IsLower(typeName[0])) // Likely a variable, not a class
                {
                    continue;
                }

                if (!result.ContainsKey(typeName))
                {
                    result[typeName] = 0;
                }
                result[typeName]++;
            }
        }

        return result;
    }
}
