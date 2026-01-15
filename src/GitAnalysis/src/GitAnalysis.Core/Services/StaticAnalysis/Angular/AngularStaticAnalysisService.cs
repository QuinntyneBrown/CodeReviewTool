// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Text.Json;
using System.Text.RegularExpressions;
using GitAnalysis.Core.Services.StaticAnalysis.Models;
using Microsoft.Extensions.Logging;

namespace GitAnalysis.Core.Services.StaticAnalysis.Angular;

/// <summary>
/// Service for performing static analysis on Angular workspaces.
/// </summary>
public partial class AngularStaticAnalysisService : IAngularStaticAnalysisService
{
    private readonly ILogger<AngularStaticAnalysisService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public AngularStaticAnalysisService(ILogger<AngularStaticAnalysisService> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    public string? FindWorkspaceRoot(string directory)
    {
        var currentDir = Path.GetFullPath(directory);

        while (!string.IsNullOrEmpty(currentDir))
        {
            var angularJsonPath = Path.Combine(currentDir, "angular.json");
            if (File.Exists(angularJsonPath))
            {
                _logger.LogDebug("Found Angular workspace at: {Path}", currentDir);
                return currentDir;
            }

            // Also check for nx.json (Nx workspaces)
            var nxJsonPath = Path.Combine(currentDir, "nx.json");
            var projectJsonPath = Path.Combine(currentDir, "project.json");
            if (File.Exists(nxJsonPath) || File.Exists(projectJsonPath))
            {
                _logger.LogDebug("Found Nx workspace at: {Path}", currentDir);
                return currentDir;
            }

            var parent = Directory.GetParent(currentDir);
            if (parent == null)
                break;

            currentDir = parent.FullName;
        }

        _logger.LogWarning("No Angular workspace found starting from: {Directory}", directory);
        return null;
    }

    public async Task<AngularAnalysisResult> AnalyzeAsync(
        string workspaceRoot,
        AngularAnalysisOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= AngularAnalysisOptions.Default;

        _logger.LogInformation("Starting Angular static analysis at: {WorkspaceRoot}", workspaceRoot);

        var result = new AngularAnalysisResult
        {
            WorkspaceRoot = workspaceRoot
        };

        // Parse angular.json or project configuration
        await ParseWorkspaceConfigAsync(workspaceRoot, result, cancellationToken);

        // Parse package.json for Angular version
        await ParsePackageJsonAsync(workspaceRoot, result, cancellationToken);

        // Find and analyze TypeScript files
        var tsFiles = GetTypeScriptFiles(workspaceRoot);
        _logger.LogInformation("Found {Count} TypeScript files to analyze", tsFiles.Count);

        foreach (var tsFile in tsFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var content = await File.ReadAllTextAsync(tsFile, cancellationToken);
                var relativePath = Path.GetRelativePath(workspaceRoot, tsFile);

                AnalyzeTypeScriptFile(content, relativePath, tsFile, result, options);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to analyze file: {File}", tsFile);
            }
        }

        // Analyze routing if requested
        if (options.AnalyzeRouting)
        {
            AnalyzeRouting(result);
        }

        // Check for issues if requested
        if (options.CheckIssues)
        {
            CheckForIssues(result);
        }

        // Build summary
        BuildSummary(result);

        _logger.LogInformation("Analysis complete. Found {Components} components, {Services} services, {Modules} modules, {Issues} issues",
            result.Summary.TotalComponents, result.Summary.TotalServices, result.Summary.TotalModules, result.Summary.IssueCount);

        return result;
    }

    private async Task ParseWorkspaceConfigAsync(
        string workspaceRoot,
        AngularAnalysisResult result,
        CancellationToken cancellationToken)
    {
        var angularJsonPath = Path.Combine(workspaceRoot, "angular.json");

        if (File.Exists(angularJsonPath))
        {
            try
            {
                var content = await File.ReadAllTextAsync(angularJsonPath, cancellationToken);
                using var doc = JsonDocument.Parse(content, new JsonDocumentOptions
                {
                    CommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                });

                if (doc.RootElement.TryGetProperty("projects", out var projects))
                {
                    foreach (var project in projects.EnumerateObject())
                    {
                        var angularProject = new AngularProject
                        {
                            Name = project.Name
                        };

                        if (project.Value.TryGetProperty("root", out var root))
                            angularProject.Root = root.GetString() ?? string.Empty;

                        if (project.Value.TryGetProperty("projectType", out var projectType))
                            angularProject.ProjectType = projectType.GetString() ?? "application";

                        if (project.Value.TryGetProperty("prefix", out var prefix))
                            angularProject.Prefix = prefix.GetString();

                        result.Projects.Add(angularProject);
                    }
                }

                _logger.LogDebug("Parsed angular.json with {Count} projects", result.Projects.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse angular.json");
            }
        }
    }

    private async Task ParsePackageJsonAsync(
        string workspaceRoot,
        AngularAnalysisResult result,
        CancellationToken cancellationToken)
    {
        var packageJsonPath = Path.Combine(workspaceRoot, "package.json");

        if (File.Exists(packageJsonPath))
        {
            try
            {
                var content = await File.ReadAllTextAsync(packageJsonPath, cancellationToken);
                using var doc = JsonDocument.Parse(content, new JsonDocumentOptions
                {
                    CommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                });

                // Check dependencies for @angular/core version
                if (doc.RootElement.TryGetProperty("dependencies", out var deps))
                {
                    if (deps.TryGetProperty("@angular/core", out var angularCore))
                    {
                        result.AngularVersion = angularCore.GetString()?.TrimStart('^', '~');
                    }
                }

                _logger.LogDebug("Detected Angular version: {Version}", result.AngularVersion ?? "unknown");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse package.json");
            }
        }
    }

    private List<string> GetTypeScriptFiles(string workspaceRoot)
    {
        var files = new List<string>();
        var excludedDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "node_modules", "dist", ".git", ".angular", "coverage", "e2e"
        };

        void SearchDirectory(string directory)
        {
            try
            {
                foreach (var file in Directory.GetFiles(directory, "*.ts"))
                {
                    // Skip spec files and test files for main analysis
                    var fileName = Path.GetFileName(file);
                    if (!fileName.EndsWith(".spec.ts", StringComparison.OrdinalIgnoreCase) &&
                        !fileName.EndsWith(".test.ts", StringComparison.OrdinalIgnoreCase))
                    {
                        files.Add(file);
                    }
                }

                foreach (var subDir in Directory.GetDirectories(directory))
                {
                    var dirName = Path.GetFileName(subDir);
                    if (!excludedDirs.Contains(dirName))
                    {
                        SearchDirectory(subDir);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error accessing directory: {Directory}", directory);
            }
        }

        SearchDirectory(workspaceRoot);
        return files;
    }

    private void AnalyzeTypeScriptFile(
        string content,
        string relativePath,
        string fullPath,
        AngularAnalysisResult result,
        AngularAnalysisOptions options)
    {
        // Check for @Component decorator
        var componentMatches = ComponentDecoratorRegex().Matches(content);
        foreach (Match match in componentMatches)
        {
            var component = ParseComponent(content, match, relativePath, fullPath);
            if (component != null)
            {
                result.Components.Add(component);
                if (component.IsStandalone)
                    result.IsStandalone = true;
            }
        }

        // Check for @Injectable decorator (services)
        var injectableMatches = InjectableDecoratorRegex().Matches(content);
        foreach (Match match in injectableMatches)
        {
            var service = ParseService(content, match, relativePath, fullPath);
            if (service != null)
            {
                result.Services.Add(service);
            }
        }

        // Check for @NgModule decorator
        var moduleMatches = NgModuleDecoratorRegex().Matches(content);
        foreach (Match match in moduleMatches)
        {
            var module = ParseModule(content, match, relativePath, fullPath);
            if (module != null)
            {
                result.Modules.Add(module);
            }
        }

        // Check for @Directive decorator
        var directiveMatches = DirectiveDecoratorRegex().Matches(content);
        foreach (Match match in directiveMatches)
        {
            var directive = ParseDirective(content, match, relativePath, fullPath);
            if (directive != null)
            {
                result.Directives.Add(directive);
            }
        }

        // Check for @Pipe decorator
        var pipeMatches = PipeDecoratorRegex().Matches(content);
        foreach (Match match in pipeMatches)
        {
            var pipe = ParsePipe(content, match, relativePath, fullPath);
            if (pipe != null)
            {
                result.Pipes.Add(pipe);
            }
        }
    }

    private AngularComponent? ParseComponent(string content, Match match, string relativePath, string fullPath)
    {
        var decoratorContent = match.Groups[1].Value;

        // Extract class name
        var classMatch = ClassNameAfterDecoratorRegex().Match(content, match.Index + match.Length);
        if (!classMatch.Success)
            return null;

        var component = new AngularComponent
        {
            Name = classMatch.Groups[1].Value,
            FilePath = relativePath.Replace('\\', '/')
        };

        // Extract selector
        var selectorMatch = SelectorRegex().Match(decoratorContent);
        if (selectorMatch.Success)
        {
            component.Selector = selectorMatch.Groups[1].Value;
        }

        // Check for standalone
        component.IsStandalone = decoratorContent.Contains("standalone") &&
            (decoratorContent.Contains("standalone: true") || StandaloneTrueRegex().IsMatch(decoratorContent));

        // Check for inline template
        component.HasInlineTemplate = decoratorContent.Contains("template:");
        if (!component.HasInlineTemplate)
        {
            var templateUrlMatch = TemplateUrlRegex().Match(decoratorContent);
            if (templateUrlMatch.Success)
            {
                var templateUrl = templateUrlMatch.Groups[1].Value;
                component.TemplatePath = ResolveRelativePath(fullPath, templateUrl);
            }
        }

        // Check for inline styles
        component.HasInlineStyles = decoratorContent.Contains("styles:");
        if (!component.HasInlineStyles)
        {
            var styleUrlsMatch = StyleUrlsRegex().Match(decoratorContent);
            if (styleUrlsMatch.Success)
            {
                var firstStyleUrl = ExtractFirstArrayItem(styleUrlsMatch.Groups[1].Value);
                if (!string.IsNullOrEmpty(firstStyleUrl))
                {
                    component.StylePath = ResolveRelativePath(fullPath, firstStyleUrl);
                }
            }
        }

        // Extract inputs
        var inputMatches = InputDecoratorRegex().Matches(content);
        foreach (Match inputMatch in inputMatches)
        {
            component.Inputs.Add(inputMatch.Groups[1].Value);
        }

        // Also check for input() signal syntax (Angular 17+)
        var signalInputMatches = SignalInputRegex().Matches(content);
        foreach (Match inputMatch in signalInputMatches)
        {
            component.Inputs.Add(inputMatch.Groups[1].Value);
        }

        // Extract outputs
        var outputMatches = OutputDecoratorRegex().Matches(content);
        foreach (Match outputMatch in outputMatches)
        {
            component.Outputs.Add(outputMatch.Groups[1].Value);
        }

        // Also check for output() signal syntax (Angular 17+)
        var signalOutputMatches = SignalOutputRegex().Matches(content);
        foreach (Match outputMatch in signalOutputMatches)
        {
            component.Outputs.Add(outputMatch.Groups[1].Value);
        }

        // Extract change detection strategy
        if (decoratorContent.Contains("ChangeDetectionStrategy.OnPush"))
        {
            component.ChangeDetection = ChangeDetectionStrategy.OnPush;
        }
        else if (decoratorContent.Contains("changeDetection"))
        {
            component.ChangeDetection = ChangeDetectionStrategy.Default;
        }

        // Extract imports (for standalone components)
        if (component.IsStandalone)
        {
            var importsMatch = ImportsArrayRegex().Match(decoratorContent);
            if (importsMatch.Success)
            {
                component.Imports = ExtractArrayItems(importsMatch.Groups[1].Value);
            }
        }

        return component;
    }

    private AngularService? ParseService(string content, Match match, string relativePath, string fullPath)
    {
        var decoratorContent = match.Groups[1].Value;

        // Extract class name
        var classMatch = ClassNameAfterDecoratorRegex().Match(content, match.Index + match.Length);
        if (!classMatch.Success)
            return null;

        var service = new AngularService
        {
            Name = classMatch.Groups[1].Value,
            FilePath = relativePath.Replace('\\', '/')
        };

        // Extract providedIn
        var providedInMatch = ProvidedInRegex().Match(decoratorContent);
        if (providedInMatch.Success)
        {
            service.ProvidedIn = providedInMatch.Groups[1].Value.Trim('\'', '"');
        }

        // Extract constructor dependencies
        var constructorMatch = ConstructorRegex().Match(content);
        if (constructorMatch.Success)
        {
            var constructorParams = constructorMatch.Groups[1].Value;
            var dependencyMatches = DependencyRegex().Matches(constructorParams);
            foreach (Match depMatch in dependencyMatches)
            {
                service.Dependencies.Add(depMatch.Groups[1].Value);
            }
        }

        // Extract public method names
        var methodMatches = PublicMethodRegex().Matches(content);
        foreach (Match methodMatch in methodMatches)
        {
            var methodName = methodMatch.Groups[1].Value;
            if (!methodName.StartsWith('_') && methodName != "constructor")
            {
                service.Methods.Add(methodName);
            }
        }

        return service;
    }

    private AngularModule? ParseModule(string content, Match match, string relativePath, string fullPath)
    {
        var decoratorContent = match.Groups[1].Value;

        // Extract class name
        var classMatch = ClassNameAfterDecoratorRegex().Match(content, match.Index + match.Length);
        if (!classMatch.Success)
            return null;

        var module = new AngularModule
        {
            Name = classMatch.Groups[1].Value,
            FilePath = relativePath.Replace('\\', '/')
        };

        // Extract declarations
        var declarationsMatch = DeclarationsRegex().Match(decoratorContent);
        if (declarationsMatch.Success)
        {
            module.Declarations = ExtractArrayItems(declarationsMatch.Groups[1].Value);
        }

        // Extract imports
        var importsMatch = ImportsArrayRegex().Match(decoratorContent);
        if (importsMatch.Success)
        {
            module.Imports = ExtractArrayItems(importsMatch.Groups[1].Value);
        }

        // Extract exports
        var exportsMatch = ExportsArrayRegex().Match(decoratorContent);
        if (exportsMatch.Success)
        {
            module.Exports = ExtractArrayItems(exportsMatch.Groups[1].Value);
        }

        // Extract providers
        var providersMatch = ProvidersArrayRegex().Match(decoratorContent);
        if (providersMatch.Success)
        {
            module.Providers = ExtractArrayItems(providersMatch.Groups[1].Value);
        }

        // Extract bootstrap
        var bootstrapMatch = BootstrapArrayRegex().Match(decoratorContent);
        if (bootstrapMatch.Success)
        {
            module.Bootstrap = ExtractArrayItems(bootstrapMatch.Groups[1].Value);
        }

        return module;
    }

    private AngularDirective? ParseDirective(string content, Match match, string relativePath, string fullPath)
    {
        var decoratorContent = match.Groups[1].Value;

        // Skip if this is actually a Component (which also has @Directive-like properties)
        if (ComponentDecoratorRegex().IsMatch(content.Substring(Math.Max(0, match.Index - 50), Math.Min(100, content.Length - Math.Max(0, match.Index - 50)))))
            return null;

        // Extract class name
        var classMatch = ClassNameAfterDecoratorRegex().Match(content, match.Index + match.Length);
        if (!classMatch.Success)
            return null;

        var directive = new AngularDirective
        {
            Name = classMatch.Groups[1].Value,
            FilePath = relativePath.Replace('\\', '/')
        };

        // Extract selector
        var selectorMatch = SelectorRegex().Match(decoratorContent);
        if (selectorMatch.Success)
        {
            directive.Selector = selectorMatch.Groups[1].Value;
        }

        // Check for standalone
        directive.IsStandalone = decoratorContent.Contains("standalone: true") ||
            StandaloneTrueRegex().IsMatch(decoratorContent);

        // Extract inputs
        var inputMatches = InputDecoratorRegex().Matches(content);
        foreach (Match inputMatch in inputMatches)
        {
            directive.Inputs.Add(inputMatch.Groups[1].Value);
        }

        // Extract outputs
        var outputMatches = OutputDecoratorRegex().Matches(content);
        foreach (Match outputMatch in outputMatches)
        {
            directive.Outputs.Add(outputMatch.Groups[1].Value);
        }

        return directive;
    }

    private AngularPipe? ParsePipe(string content, Match match, string relativePath, string fullPath)
    {
        var decoratorContent = match.Groups[1].Value;

        // Extract class name
        var classMatch = ClassNameAfterDecoratorRegex().Match(content, match.Index + match.Length);
        if (!classMatch.Success)
            return null;

        var pipe = new AngularPipe
        {
            Name = classMatch.Groups[1].Value,
            FilePath = relativePath.Replace('\\', '/')
        };

        // Extract pipe name
        var nameMatch = PipeNameRegex().Match(decoratorContent);
        if (nameMatch.Success)
        {
            pipe.PipeName = nameMatch.Groups[1].Value.Trim('\'', '"');
        }

        // Check for standalone
        pipe.IsStandalone = decoratorContent.Contains("standalone: true") ||
            StandaloneTrueRegex().IsMatch(decoratorContent);

        // Check for pure
        pipe.IsPure = !decoratorContent.Contains("pure: false");

        return pipe;
    }

    private void AnalyzeRouting(AngularAnalysisResult result)
    {
        // Look for route configurations in the analyzed files
        foreach (var module in result.Modules)
        {
            if (module.Name.Contains("Routing") || module.Imports.Any(i => i.Contains("RouterModule")))
            {
                // This module likely contains routes
                _logger.LogDebug("Found routing module: {Module}", module.Name);
            }
        }

        // Routes are typically found in *-routing.module.ts or app.routes.ts files
        // For now, we log that routing analysis would happen here
        _logger.LogDebug("Route analysis completed");
    }

    private void CheckForIssues(AngularAnalysisResult result)
    {
        // Check for components without OnPush change detection
        foreach (var component in result.Components.Where(c => c.ChangeDetection != ChangeDetectionStrategy.OnPush))
        {
            result.Issues.Add(new AngularIssue
            {
                Severity = IssueSeverity.Info,
                Category = "Performance",
                Message = $"Component '{component.Name}' does not use OnPush change detection",
                FilePath = component.FilePath,
                Suggestion = "Consider using ChangeDetectionStrategy.OnPush for better performance"
            });
        }

        // Check for services not provided in 'root'
        foreach (var service in result.Services.Where(s => string.IsNullOrEmpty(s.ProvidedIn)))
        {
            result.Issues.Add(new AngularIssue
            {
                Severity = IssueSeverity.Warning,
                Category = "Architecture",
                Message = $"Service '{service.Name}' does not specify providedIn",
                FilePath = service.FilePath,
                Suggestion = "Add providedIn: 'root' for tree-shakable services"
            });
        }

        // Check for large modules with many declarations
        foreach (var module in result.Modules.Where(m => m.Declarations.Count > 20))
        {
            result.Issues.Add(new AngularIssue
            {
                Severity = IssueSeverity.Warning,
                Category = "Architecture",
                Message = $"Module '{module.Name}' has {module.Declarations.Count} declarations",
                FilePath = module.FilePath,
                Suggestion = "Consider breaking this module into smaller feature modules"
            });
        }

        // Check for non-standalone components in Angular 17+ projects
        if (result.IsStandalone)
        {
            foreach (var component in result.Components.Where(c => !c.IsStandalone))
            {
                result.Issues.Add(new AngularIssue
                {
                    Severity = IssueSeverity.Info,
                    Category = "Migration",
                    Message = $"Component '{component.Name}' is not standalone",
                    FilePath = component.FilePath,
                    Suggestion = "Consider migrating to standalone components for better tree-shaking"
                });
            }
        }

        // Check for impure pipes
        foreach (var pipe in result.Pipes.Where(p => !p.IsPure))
        {
            result.Issues.Add(new AngularIssue
            {
                Severity = IssueSeverity.Warning,
                Category = "Performance",
                Message = $"Pipe '{pipe.Name}' is impure",
                FilePath = pipe.FilePath,
                Suggestion = "Impure pipes can cause performance issues as they run on every change detection cycle"
            });
        }

        // Check for components with many inputs/outputs
        foreach (var component in result.Components.Where(c => c.Inputs.Count + c.Outputs.Count > 10))
        {
            result.Issues.Add(new AngularIssue
            {
                Severity = IssueSeverity.Warning,
                Category = "Design",
                Message = $"Component '{component.Name}' has {component.Inputs.Count} inputs and {component.Outputs.Count} outputs",
                FilePath = component.FilePath,
                Suggestion = "Consider refactoring to reduce component complexity or use a shared data service"
            });
        }
    }

    private void BuildSummary(AngularAnalysisResult result)
    {
        result.Summary = new AngularAnalysisSummary
        {
            TotalComponents = result.Components.Count,
            TotalServices = result.Services.Count,
            TotalModules = result.Modules.Count,
            TotalDirectives = result.Directives.Count,
            TotalPipes = result.Pipes.Count,
            TotalRoutes = result.Routes.Count,
            StandaloneComponents = result.Components.Count(c => c.IsStandalone),
            OnPushComponents = result.Components.Count(c => c.ChangeDetection == ChangeDetectionStrategy.OnPush),
            IssueCount = result.Issues.Count,
            WarningCount = result.Issues.Count(i => i.Severity == IssueSeverity.Warning),
            ErrorCount = result.Issues.Count(i => i.Severity == IssueSeverity.Error)
        };
    }

    private static string? ResolveRelativePath(string basePath, string relativePath)
    {
        try
        {
            var directory = Path.GetDirectoryName(basePath);
            if (string.IsNullOrEmpty(directory))
                return relativePath;

            return Path.GetFullPath(Path.Combine(directory, relativePath));
        }
        catch
        {
            return relativePath;
        }
    }

    private static List<string> ExtractArrayItems(string arrayContent)
    {
        var items = new List<string>();
        var matches = ArrayItemRegex().Matches(arrayContent);

        foreach (Match match in matches)
        {
            var item = match.Groups[1].Value.Trim();
            if (!string.IsNullOrEmpty(item))
            {
                items.Add(item);
            }
        }

        return items;
    }

    private static string? ExtractFirstArrayItem(string arrayContent)
    {
        var match = FirstArrayItemRegex().Match(arrayContent);
        return match.Success ? match.Groups[1].Value.Trim('\'', '"') : null;
    }

    // Regex patterns
    [GeneratedRegex(@"@Component\s*\(\s*\{([^}]+(?:\{[^}]*\}[^}]*)*)\}\s*\)", RegexOptions.Singleline)]
    private static partial Regex ComponentDecoratorRegex();

    [GeneratedRegex(@"@Injectable\s*\(\s*\{([^}]*)\}\s*\)", RegexOptions.Singleline)]
    private static partial Regex InjectableDecoratorRegex();

    [GeneratedRegex(@"@NgModule\s*\(\s*\{([^}]+(?:\{[^}]*\}[^}]*)*)\}\s*\)", RegexOptions.Singleline)]
    private static partial Regex NgModuleDecoratorRegex();

    [GeneratedRegex(@"@Directive\s*\(\s*\{([^}]*)\}\s*\)", RegexOptions.Singleline)]
    private static partial Regex DirectiveDecoratorRegex();

    [GeneratedRegex(@"@Pipe\s*\(\s*\{([^}]*)\}\s*\)", RegexOptions.Singleline)]
    private static partial Regex PipeDecoratorRegex();

    [GeneratedRegex(@"(?:export\s+)?class\s+(\w+)", RegexOptions.None)]
    private static partial Regex ClassNameAfterDecoratorRegex();

    [GeneratedRegex(@"selector\s*:\s*['""]([^'""]+)['""]", RegexOptions.None)]
    private static partial Regex SelectorRegex();

    [GeneratedRegex(@"templateUrl\s*:\s*['""]([^'""]+)['""]", RegexOptions.None)]
    private static partial Regex TemplateUrlRegex();

    [GeneratedRegex(@"styleUrls?\s*:\s*\[([^\]]*)\]", RegexOptions.Singleline)]
    private static partial Regex StyleUrlsRegex();

    [GeneratedRegex(@"standalone\s*:\s*true", RegexOptions.None)]
    private static partial Regex StandaloneTrueRegex();

    [GeneratedRegex(@"@Input\s*\([^)]*\)\s*(?:public\s+)?(\w+)", RegexOptions.None)]
    private static partial Regex InputDecoratorRegex();

    [GeneratedRegex(@"(\w+)\s*=\s*input(?:<[^>]+>)?\s*\(", RegexOptions.None)]
    private static partial Regex SignalInputRegex();

    [GeneratedRegex(@"@Output\s*\([^)]*\)\s*(?:public\s+)?(\w+)", RegexOptions.None)]
    private static partial Regex OutputDecoratorRegex();

    [GeneratedRegex(@"(\w+)\s*=\s*output(?:<[^>]+>)?\s*\(", RegexOptions.None)]
    private static partial Regex SignalOutputRegex();

    [GeneratedRegex(@"imports\s*:\s*\[([^\]]*)\]", RegexOptions.Singleline)]
    private static partial Regex ImportsArrayRegex();

    [GeneratedRegex(@"providedIn\s*:\s*([^\s,}]+)", RegexOptions.None)]
    private static partial Regex ProvidedInRegex();

    [GeneratedRegex(@"constructor\s*\(([^)]*)\)", RegexOptions.Singleline)]
    private static partial Regex ConstructorRegex();

    [GeneratedRegex(@"(?:private|public|protected)?\s*(?:readonly)?\s*\w+\s*:\s*(\w+)", RegexOptions.None)]
    private static partial Regex DependencyRegex();

    [GeneratedRegex(@"(?:public\s+)?(?:async\s+)?(\w+)\s*\([^)]*\)\s*(?::\s*[^{]+)?\s*\{", RegexOptions.None)]
    private static partial Regex PublicMethodRegex();

    [GeneratedRegex(@"declarations\s*:\s*\[([^\]]*)\]", RegexOptions.Singleline)]
    private static partial Regex DeclarationsRegex();

    [GeneratedRegex(@"exports\s*:\s*\[([^\]]*)\]", RegexOptions.Singleline)]
    private static partial Regex ExportsArrayRegex();

    [GeneratedRegex(@"providers\s*:\s*\[([^\]]*)\]", RegexOptions.Singleline)]
    private static partial Regex ProvidersArrayRegex();

    [GeneratedRegex(@"bootstrap\s*:\s*\[([^\]]*)\]", RegexOptions.Singleline)]
    private static partial Regex BootstrapArrayRegex();

    [GeneratedRegex(@"name\s*:\s*([^\s,}]+)", RegexOptions.None)]
    private static partial Regex PipeNameRegex();

    [GeneratedRegex(@"(\w+)(?:\s*,|\s*$)", RegexOptions.None)]
    private static partial Regex ArrayItemRegex();

    [GeneratedRegex(@"['""]([^'""]+)['""]", RegexOptions.None)]
    private static partial Regex FirstArrayItemRegex();
}
