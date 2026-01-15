// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using GitAnalysis.Core.Services.StaticAnalysis.Models;

namespace GitAnalysis.Core.Services.StaticAnalysis.Angular;

/// <summary>
/// Service for performing static analysis on Angular workspaces.
/// </summary>
public interface IAngularStaticAnalysisService
{
    /// <summary>
    /// Analyzes an Angular workspace and returns detailed analysis results.
    /// </summary>
    /// <param name="workspaceRoot">The root directory of the Angular workspace.</param>
    /// <param name="options">Optional analysis options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Analysis results containing components, services, modules, and issues.</returns>
    Task<AngularAnalysisResult> AnalyzeAsync(
        string workspaceRoot,
        AngularAnalysisOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds the Angular workspace root from a given directory.
    /// </summary>
    /// <param name="directory">The starting directory to search from.</param>
    /// <returns>The workspace root directory, or null if not found.</returns>
    string? FindWorkspaceRoot(string directory);
}

/// <summary>
/// Options for Angular static analysis.
/// </summary>
public class AngularAnalysisOptions
{
    /// <summary>
    /// Whether to include detailed template analysis.
    /// </summary>
    public bool AnalyzeTemplates { get; set; } = true;

    /// <summary>
    /// Whether to include style analysis.
    /// </summary>
    public bool AnalyzeStyles { get; set; } = true;

    /// <summary>
    /// Whether to check for common issues and anti-patterns.
    /// </summary>
    public bool CheckIssues { get; set; } = true;

    /// <summary>
    /// Whether to analyze routing configuration.
    /// </summary>
    public bool AnalyzeRouting { get; set; } = true;

    /// <summary>
    /// Maximum depth for dependency analysis.
    /// </summary>
    public int MaxDependencyDepth { get; set; } = 5;

    /// <summary>
    /// Creates default options.
    /// </summary>
    public static AngularAnalysisOptions Default => new();
}

/// <summary>
/// Result of Angular static analysis.
/// </summary>
public class AngularAnalysisResult
{
    /// <summary>
    /// The workspace root directory that was analyzed.
    /// </summary>
    public string WorkspaceRoot { get; set; } = string.Empty;

    /// <summary>
    /// The Angular version detected from package.json.
    /// </summary>
    public string? AngularVersion { get; set; }

    /// <summary>
    /// Whether this is a standalone component-based project (Angular 14+).
    /// </summary>
    public bool IsStandalone { get; set; }

    /// <summary>
    /// Projects found in the workspace (for multi-project workspaces).
    /// </summary>
    public List<AngularProject> Projects { get; set; } = [];

    /// <summary>
    /// All components found in the workspace.
    /// </summary>
    public List<AngularComponent> Components { get; set; } = [];

    /// <summary>
    /// All services found in the workspace.
    /// </summary>
    public List<AngularService> Services { get; set; } = [];

    /// <summary>
    /// All modules found in the workspace.
    /// </summary>
    public List<AngularModule> Modules { get; set; } = [];

    /// <summary>
    /// All directives found in the workspace.
    /// </summary>
    public List<AngularDirective> Directives { get; set; } = [];

    /// <summary>
    /// All pipes found in the workspace.
    /// </summary>
    public List<AngularPipe> Pipes { get; set; } = [];

    /// <summary>
    /// Route configurations found.
    /// </summary>
    public List<AngularRoute> Routes { get; set; } = [];

    /// <summary>
    /// Issues and warnings found during analysis.
    /// </summary>
    public List<AngularIssue> Issues { get; set; } = [];

    /// <summary>
    /// Summary statistics.
    /// </summary>
    public AngularAnalysisSummary Summary { get; set; } = new();

    /// <summary>
    /// Analysis timestamp.
    /// </summary>
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents an Angular project in a workspace.
/// </summary>
public class AngularProject
{
    public string Name { get; set; } = string.Empty;
    public string Root { get; set; } = string.Empty;
    public string ProjectType { get; set; } = string.Empty; // application, library
    public string? Prefix { get; set; }
}

/// <summary>
/// Represents an Angular component.
/// </summary>
public class AngularComponent
{
    public string Name { get; set; } = string.Empty;
    public string Selector { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string? TemplatePath { get; set; }
    public string? StylePath { get; set; }
    public bool IsStandalone { get; set; }
    public bool HasInlineTemplate { get; set; }
    public bool HasInlineStyles { get; set; }
    public List<string> Inputs { get; set; } = [];
    public List<string> Outputs { get; set; } = [];
    public List<string> Imports { get; set; } = [];
    public List<string> Providers { get; set; } = [];
    public ChangeDetectionStrategy? ChangeDetection { get; set; }
}

/// <summary>
/// Angular change detection strategies.
/// </summary>
public enum ChangeDetectionStrategy
{
    Default,
    OnPush
}

/// <summary>
/// Represents an Angular service.
/// </summary>
public class AngularService
{
    public string Name { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string? ProvidedIn { get; set; }
    public List<string> Dependencies { get; set; } = [];
    public List<string> Methods { get; set; } = [];
}

/// <summary>
/// Represents an Angular module.
/// </summary>
public class AngularModule
{
    public string Name { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public List<string> Declarations { get; set; } = [];
    public List<string> Imports { get; set; } = [];
    public List<string> Exports { get; set; } = [];
    public List<string> Providers { get; set; } = [];
    public List<string> Bootstrap { get; set; } = [];
}

/// <summary>
/// Represents an Angular directive.
/// </summary>
public class AngularDirective
{
    public string Name { get; set; } = string.Empty;
    public string Selector { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public bool IsStandalone { get; set; }
    public List<string> Inputs { get; set; } = [];
    public List<string> Outputs { get; set; } = [];
}

/// <summary>
/// Represents an Angular pipe.
/// </summary>
public class AngularPipe
{
    public string Name { get; set; } = string.Empty;
    public string PipeName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public bool IsStandalone { get; set; }
    public bool IsPure { get; set; } = true;
}

/// <summary>
/// Represents an Angular route configuration.
/// </summary>
public class AngularRoute
{
    public string Path { get; set; } = string.Empty;
    public string? Component { get; set; }
    public string? LoadComponent { get; set; }
    public string? LoadChildren { get; set; }
    public string? RedirectTo { get; set; }
    public bool HasGuards { get; set; }
    public bool HasResolvers { get; set; }
    public List<AngularRoute> Children { get; set; } = [];
}

/// <summary>
/// Represents an issue found during analysis.
/// </summary>
public class AngularIssue
{
    public IssueSeverity Severity { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? FilePath { get; set; }
    public int? Line { get; set; }
    public string? Suggestion { get; set; }
}

/// <summary>
/// Summary statistics for the analysis.
/// </summary>
public class AngularAnalysisSummary
{
    public int TotalComponents { get; set; }
    public int TotalServices { get; set; }
    public int TotalModules { get; set; }
    public int TotalDirectives { get; set; }
    public int TotalPipes { get; set; }
    public int TotalRoutes { get; set; }
    public int StandaloneComponents { get; set; }
    public int OnPushComponents { get; set; }
    public int IssueCount { get; set; }
    public int WarningCount { get; set; }
    public int ErrorCount { get; set; }
}
