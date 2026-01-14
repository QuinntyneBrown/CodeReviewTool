using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using StaticCodeAnalyzer.Analysis.Analyzers.Security;
using StaticCodeAnalyzer.Analysis.Analyzers.Reliability;
using StaticCodeAnalyzer.Analysis.Analyzers.Maintainability;
using StaticCodeAnalyzer.Analysis.Analyzers.CodeSmells;

namespace StaticCodeAnalyzer.Analysis;

public class AnalysisEngine
{
    private readonly List<ICodeAnalyzer> _analyzers;

    public AnalysisEngine()
    {
        _analyzers = new List<ICodeAnalyzer>
        {
            // Security Analyzers (Critical for Safety-Critical Profile)
            new SqlInjectionAnalyzer(),
            new CommandInjectionAnalyzer(),
            new XssAnalyzer(),
            new PathTraversalAnalyzer(),
            new HardcodedCredentialsAnalyzer(),
            new InsecureDeserializationAnalyzer(),
            new XxeAnalyzer(),
            new WeakCryptographyAnalyzer(),
            new InsecureRandomAnalyzer(),
            new LdapInjectionAnalyzer(),
            new OpenRedirectAnalyzer(),
            new SensitiveDataExposureAnalyzer(),

            // Reliability Analyzers
            new NullReferenceAnalyzer(),
            new ResourceLeakAnalyzer(),
            new EmptyCatchBlockAnalyzer(),
            new ExceptionHandlingAnalyzer(),
            new ThreadSafetyAnalyzer(),
            new InfiniteLoopAnalyzer(),
            new ArrayBoundsAnalyzer(),

            // Maintainability Analyzers
            new CyclomaticComplexityAnalyzer(),
            new MethodLengthAnalyzer(),
            new ClassLengthAnalyzer(),
            new ParameterCountAnalyzer(),
            new NestingDepthAnalyzer(),
            new CognitiveComplexityAnalyzer(),

            // Code Smell Analyzers
            new UnusedVariableAnalyzer(),
            new MagicNumberAnalyzer(),
            new EmptyBlockAnalyzer(),
            new DuplicateCodeAnalyzer(),
            new LongParameterListAnalyzer(),
            new GodClassAnalyzer()
        };
    }

    public async Task<List<AnalysisResult>> AnalyzeFileAsync(string filePath)
    {
        var results = new List<AnalysisResult>();

        string sourceCode = await File.ReadAllTextAsync(filePath);
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode, path: filePath);

        // Create a basic compilation for semantic analysis
        var compilation = CSharpCompilation.Create("Analysis")
            .AddReferences(GetDefaultReferences())
            .AddSyntaxTrees(syntaxTree);

        var semanticModel = compilation.GetSemanticModel(syntaxTree);

        foreach (var analyzer in _analyzers)
        {
            try
            {
                var analyzerResults = await analyzer.AnalyzeAsync(syntaxTree, semanticModel, filePath);
                results.AddRange(analyzerResults);
            }
            catch (Exception ex)
            {
                // Log but don't fail the entire analysis
                Console.Error.WriteLine($"Analyzer {analyzer.Name} failed: {ex.Message}");
            }
        }

        return results;
    }

    private static IEnumerable<MetadataReference> GetDefaultReferences()
    {
        var assemblies = new[]
        {
            typeof(object).Assembly,
            typeof(Console).Assembly,
            typeof(Enumerable).Assembly
        };

        foreach (var assembly in assemblies)
        {
            yield return MetadataReference.CreateFromFile(assembly.Location);
        }

        // Add runtime assemblies
        var runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        var runtimeAssemblies = new[]
        {
            "System.Runtime.dll",
            "System.Collections.dll",
            "System.Linq.dll",
            "System.Threading.dll",
            "System.Threading.Tasks.dll"
        };

        foreach (var asm in runtimeAssemblies)
        {
            var path = Path.Combine(runtimeDir, asm);
            if (File.Exists(path))
            {
                yield return MetadataReference.CreateFromFile(path);
            }
        }
    }
}
