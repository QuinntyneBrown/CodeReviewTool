// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Text.RegularExpressions;
using GitAnalysis.Core.Services.StaticAnalysis.Models;
using Microsoft.Extensions.Logging;

namespace GitAnalysis.Core.Services.StaticAnalysis.Scss;

/// <summary>
/// Service for performing static analysis on SCSS files.
/// Analyzes SCSS code for common issues, best practices, and potential errors.
/// </summary>
public partial class ScssStaticAnalysisService : IScssStaticAnalysisService
{
    private readonly ILogger<ScssStaticAnalysisService> _logger;
    private const int MaxRecommendedNestingDepth = 3;
    private const int MaxRecommendedSelectorComplexity = 4;

    public ScssStaticAnalysisService(ILogger<ScssStaticAnalysisService> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<ScssAnalysisResult> AnalyzeFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filePath);

        _logger.LogInformation("Analyzing SCSS file: {FilePath}", filePath);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"SCSS file not found: {filePath}", filePath);
        }

        var content = await File.ReadAllTextAsync(filePath, cancellationToken);
        var result = AnalyzeContent(content, filePath);
        result.FilePath = filePath;

        return result;
    }

    /// <inheritdoc/>
    public async Task<ScssDirectoryAnalysisResult> AnalyzeDirectoryAsync(string directoryPath, bool recursive = true, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(directoryPath);

        _logger.LogInformation("Analyzing SCSS files in directory: {DirectoryPath} (recursive: {Recursive})", directoryPath, recursive);

        if (!Directory.Exists(directoryPath))
        {
            throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");
        }

        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var scssFiles = Directory.GetFiles(directoryPath, "*.scss", searchOption)
            .Concat(Directory.GetFiles(directoryPath, "*.sass", searchOption))
            .ToArray();

        _logger.LogInformation("Found {Count} SCSS files to analyze", scssFiles.Length);

        var result = new ScssDirectoryAnalysisResult
        {
            DirectoryPath = directoryPath
        };

        foreach (var file in scssFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var fileResult = await AnalyzeFileAsync(file, cancellationToken);
                result.FileResults.Add(fileResult);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to analyze file: {FilePath}", file);
                result.FileResults.Add(new ScssAnalysisResult
                {
                    FilePath = file,
                    Issues = new List<ScssIssue>
                    {
                        new ScssIssue
                        {
                            Line = 0,
                            Column = 0,
                            Code = "SCSS001",
                            Message = $"Failed to analyze file: {ex.Message}",
                            Severity = IssueSeverity.Error,
                            Rule = "file-analysis"
                        }
                    }
                });
            }
        }

        _logger.LogInformation("Analysis complete. Found {TotalIssues} issues across {TotalFiles} files",
            result.TotalIssues, result.TotalFiles);

        return result;
    }

    /// <inheritdoc/>
    public ScssAnalysisResult AnalyzeContent(string content, string fileName = "input.scss")
    {
        ArgumentNullException.ThrowIfNull(content);

        // Normalize line endings for consistent regex matching
        content = content.Replace("\r\n", "\n").Replace("\r", "\n");

        var result = new ScssAnalysisResult
        {
            FilePath = fileName
        };

        var lines = content.Split('\n');
        result.TotalLines = lines.Length;

        // Run all analysis checks
        CheckNestingDepth(content, lines, result);
        CheckEmptyRules(content, lines, result);
        CheckDuplicateSelectors(content, lines, result);
        CheckColorFormats(content, lines, result);
        CheckImportUsage(content, lines, result);
        CheckImportantUsage(content, lines, result);
        CheckIdSelectors(content, lines, result);
        CheckSelectorComplexity(content, lines, result);
        CheckMissingSemicolons(content, lines, result);
        CheckZeroUnits(content, lines, result);
        CheckHardcodedColors(content, lines, result);
        CheckVendorPrefixes(content, lines, result);
        CollectMetrics(content, result);

        return result;
    }

    private void CheckNestingDepth(string content, string[] lines, ScssAnalysisResult result)
    {
        int currentDepth = 0;
        int maxDepth = 0;
        int maxDepthLine = 0;

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];

            // Skip comments
            if (line.TrimStart().StartsWith("//") || line.TrimStart().StartsWith("/*"))
            {
                continue;
            }

            currentDepth += line.Count(c => c == '{');
            currentDepth -= line.Count(c => c == '}');

            if (currentDepth > maxDepth)
            {
                maxDepth = currentDepth;
                maxDepthLine = i + 1;
            }
        }

        result.MaxNestingDepth = maxDepth;

        if (maxDepth > MaxRecommendedNestingDepth)
        {
            result.Issues.Add(new ScssIssue
            {
                Line = maxDepthLine,
                Column = 1,
                Code = "SCSS100",
                Message = $"Nesting depth of {maxDepth} exceeds recommended maximum of {MaxRecommendedNestingDepth}. Deep nesting can lead to overly specific selectors and maintainability issues.",
                Severity = IssueSeverity.Warning,
                Rule = "max-nesting-depth"
            });
        }
    }

    private void CheckEmptyRules(string content, string[] lines, ScssAnalysisResult result)
    {
        var matches = EmptyRuleRegex().Matches(content);

        foreach (Match match in matches)
        {
            var lineNumber = GetLineNumber(content, match.Index);
            result.Issues.Add(new ScssIssue
            {
                Line = lineNumber,
                Column = 1,
                Code = "SCSS101",
                Message = "Empty rule block detected. Consider removing it or adding styles.",
                Severity = IssueSeverity.Warning,
                Rule = "no-empty-rules",
                SourceSnippet = match.Value.Trim()
            });
        }
    }

    private void CheckDuplicateSelectors(string content, string[] lines, ScssAnalysisResult result)
    {
        var selectorMatches = SelectorRegex().Matches(content);
        var selectors = new Dictionary<string, List<int>>();

        foreach (Match match in selectorMatches)
        {
            var selector = NormalizeSelector(match.Groups[1].Value);

            // Skip empty selectors or selectors that are just whitespace
            if (string.IsNullOrWhiteSpace(selector))
            {
                continue;
            }

            var lineNumber = GetLineNumber(content, match.Index);

            if (!selectors.ContainsKey(selector))
            {
                selectors[selector] = new List<int>();
            }

            selectors[selector].Add(lineNumber);
        }

        result.SelectorCount = selectors.Count;

        foreach (var kvp in selectors.Where(s => s.Value.Count > 1))
        {
            result.Issues.Add(new ScssIssue
            {
                Line = kvp.Value.Last(),
                Column = 1,
                Code = "SCSS102",
                Message = $"Duplicate selector '{kvp.Key}' found on lines: {string.Join(", ", kvp.Value)}. Consider consolidating these rules.",
                Severity = IssueSeverity.Warning,
                Rule = "no-duplicate-selectors",
                SourceSnippet = kvp.Key
            });
        }
    }

    private void CheckColorFormats(string content, string[] lines, ScssAnalysisResult result)
    {
        // Check for inconsistent color formats (hex vs rgb vs named)
        var hexColors = HexColorRegex().Matches(content).Count;
        var rgbColors = RgbColorRegex().Matches(content).Count;
        var namedColors = NamedColorRegex().Matches(content).Count;

        if (hexColors > 0 && rgbColors > 0 && namedColors > 0)
        {
            result.Issues.Add(new ScssIssue
            {
                Line = 1,
                Column = 1,
                Code = "SCSS103",
                Message = $"Inconsistent color formats detected (hex: {hexColors}, rgb: {rgbColors}, named: {namedColors}). Consider using a consistent color format or SCSS variables.",
                Severity = IssueSeverity.Info,
                Rule = "color-format-consistency"
            });
        }

        // Check for short hex that could be expanded
        foreach (Match match in ShortHexColorRegex().Matches(content))
        {
            var lineNumber = GetLineNumber(content, match.Index);
            result.Issues.Add(new ScssIssue
            {
                Line = lineNumber,
                Column = 1,
                Code = "SCSS104",
                Message = $"Consider using 6-digit hex color instead of shorthand '{match.Value}' for consistency.",
                Severity = IssueSeverity.Info,
                Rule = "color-hex-length",
                SourceSnippet = match.Value
            });
        }
    }

    private void CheckImportUsage(string content, string[] lines, ScssAnalysisResult result)
    {
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (line.StartsWith("@import") && !line.Contains("url("))
            {
                result.Issues.Add(new ScssIssue
                {
                    Line = i + 1,
                    Column = 1,
                    Code = "SCSS105",
                    Message = "Use of @import is deprecated. Consider using @use or @forward instead for better modularity and to avoid duplicate imports.",
                    Severity = IssueSeverity.Warning,
                    Rule = "no-import",
                    SourceSnippet = line
                });
            }
        }
    }

    private void CheckImportantUsage(string content, string[] lines, ScssAnalysisResult result)
    {
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (line.Contains("!important"))
            {
                result.Issues.Add(new ScssIssue
                {
                    Line = i + 1,
                    Column = line.IndexOf("!important") + 1,
                    Code = "SCSS106",
                    Message = "Avoid using !important as it makes styles difficult to override and maintain. Consider refactoring selector specificity instead.",
                    Severity = IssueSeverity.Warning,
                    Rule = "no-important",
                    SourceSnippet = line.Trim()
                });
            }
        }
    }

    private void CheckIdSelectors(string content, string[] lines, ScssAnalysisResult result)
    {
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];

            // Skip if inside a comment or string
            if (line.TrimStart().StartsWith("//") || line.TrimStart().StartsWith("/*"))
            {
                continue;
            }

            var matches = IdSelectorRegex().Matches(line);
            foreach (Match match in matches)
            {
                // Skip interpolation like #{$var}
                if (match.Index > 0 && line[match.Index - 1] == '{')
                {
                    continue;
                }

                // Skip if this # appears after a colon (property value context - likely a hex color)
                var beforeMatch = line.Substring(0, match.Index);
                if (beforeMatch.Contains(':'))
                {
                    continue;
                }

                // Skip if it looks like a hex color (only hex digits 0-9 and a-f after the #)
                var value = match.Value.Substring(1); // Remove the #
                if (value.Length >= 3 && value.Length <= 8 && value.All(c => "0123456789abcdefABCDEF".Contains(c)))
                {
                    continue;
                }

                result.Issues.Add(new ScssIssue
                {
                    Line = i + 1,
                    Column = match.Index + 1,
                    Code = "SCSS107",
                    Message = $"Avoid using ID selectors '{match.Value}' as they have high specificity. Consider using classes instead.",
                    Severity = IssueSeverity.Info,
                    Rule = "no-id-selectors",
                    SourceSnippet = match.Value
                });
            }
        }
    }

    private void CheckSelectorComplexity(string content, string[] lines, ScssAnalysisResult result)
    {
        var matches = SelectorRegex().Matches(content);

        foreach (Match match in matches)
        {
            var selector = match.Groups[1].Value.Trim();
            var complexity = CountSelectorParts(selector);

            if (complexity > MaxRecommendedSelectorComplexity)
            {
                var lineNumber = GetLineNumber(content, match.Index);
                result.Issues.Add(new ScssIssue
                {
                    Line = lineNumber,
                    Column = 1,
                    Code = "SCSS108",
                    Message = $"Selector complexity of {complexity} exceeds recommended maximum of {MaxRecommendedSelectorComplexity}. Complex selectors can impact performance and maintainability.",
                    Severity = IssueSeverity.Warning,
                    Rule = "selector-complexity",
                    SourceSnippet = selector
                });
            }
        }
    }

    private void CheckMissingSemicolons(string content, string[] lines, ScssAnalysisResult result)
    {
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();

            // Skip empty lines, comments, and lines with braces
            if (string.IsNullOrWhiteSpace(line) ||
                line.StartsWith("//") ||
                line.StartsWith("/*") ||
                line.StartsWith("*") ||
                line.EndsWith("{") ||
                line.EndsWith("}") ||
                line.StartsWith("@") ||
                line.StartsWith("$") && !line.Contains(":"))
            {
                continue;
            }

            // Check for property declarations without semicolons
            if (line.Contains(":") && !line.EndsWith(";") && !line.EndsWith("{") && !line.EndsWith(","))
            {
                // Could be multiline, check if next non-empty line starts with a property or selector
                bool isLastLineOfBlock = i + 1 < lines.Length &&
                    (lines[i + 1].Trim().StartsWith("}") || lines[i + 1].Trim().EndsWith("{"));

                if (isLastLineOfBlock)
                {
                    result.Issues.Add(new ScssIssue
                    {
                        Line = i + 1,
                        Column = line.Length,
                        Code = "SCSS109",
                        Message = "Missing semicolon at end of declaration.",
                        Severity = IssueSeverity.Error,
                        Rule = "declaration-semicolon",
                        SourceSnippet = line
                    });
                }
            }
        }
    }

    private void CheckZeroUnits(string content, string[] lines, ScssAnalysisResult result)
    {
        foreach (Match match in ZeroWithUnitRegex().Matches(content))
        {
            var lineNumber = GetLineNumber(content, match.Index);
            result.Issues.Add(new ScssIssue
            {
                Line = lineNumber,
                Column = 1,
                Code = "SCSS110",
                Message = $"Unnecessary unit on zero value '{match.Value}'. Zero is zero regardless of unit.",
                Severity = IssueSeverity.Info,
                Rule = "zero-units",
                SourceSnippet = match.Value
            });
        }
    }

    private void CheckHardcodedColors(string content, string[] lines, ScssAnalysisResult result)
    {
        var colorMatches = AllColorsRegex().Matches(content);
        var colorUsage = new Dictionary<string, int>();

        foreach (Match match in colorMatches)
        {
            var color = match.Value.ToLowerInvariant();
            colorUsage[color] = colorUsage.GetValueOrDefault(color) + 1;
        }

        var repeatedColors = colorUsage.Where(c => c.Value > 2).ToList();
        if (repeatedColors.Any())
        {
            result.Issues.Add(new ScssIssue
            {
                Line = 1,
                Column = 1,
                Code = "SCSS111",
                Message = $"Hardcoded colors used multiple times: {string.Join(", ", repeatedColors.Select(c => $"'{c.Key}' ({c.Value}x)"))}. Consider extracting these to variables.",
                Severity = IssueSeverity.Info,
                Rule = "no-hardcoded-colors"
            });
        }
    }

    private void CheckVendorPrefixes(string content, string[] lines, ScssAnalysisResult result)
    {
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (VendorPrefixRegex().IsMatch(line))
            {
                result.Issues.Add(new ScssIssue
                {
                    Line = i + 1,
                    Column = 1,
                    Code = "SCSS112",
                    Message = "Vendor prefixes detected. Consider using autoprefixer or a mixin library instead of manually adding vendor prefixes.",
                    Severity = IssueSeverity.Info,
                    Rule = "no-vendor-prefixes",
                    SourceSnippet = line.Trim()
                });
            }
        }
    }

    private void CollectMetrics(string content, ScssAnalysisResult result)
    {
        // Count variables
        result.VariableCount = VariableDeclarationRegex().Matches(content).Count;

        // Count mixins
        result.MixinCount = MixinDeclarationRegex().Matches(content).Count;
    }

    private static int GetLineNumber(string content, int position)
    {
        return content.Substring(0, position).Count(c => c == '\n') + 1;
    }

    private static string NormalizeSelector(string selector)
    {
        return MultipleWhitespaceRegex().Replace(selector.Trim(), " ");
    }

    private static int CountSelectorParts(string selector)
    {
        // Count combinators and selectors
        return selector.Split(new[] { ' ', '>', '+', '~' }, StringSplitOptions.RemoveEmptyEntries).Length;
    }

    // Generated regex patterns for performance
    [GeneratedRegex(@"[^}]*\{\s*\}", RegexOptions.Multiline)]
    private static partial Regex EmptyRuleRegex();

    [GeneratedRegex(@"^([^{@/\r\n]+)\{", RegexOptions.Multiline)]
    private static partial Regex SelectorRegex();

    [GeneratedRegex(@"#[0-9a-fA-F]{3,8}\b")]
    private static partial Regex HexColorRegex();

    [GeneratedRegex(@"#[0-9a-fA-F]{3}\b(?![0-9a-fA-F])")]
    private static partial Regex ShortHexColorRegex();

    [GeneratedRegex(@"rgba?\s*\([^)]+\)", RegexOptions.IgnoreCase)]
    private static partial Regex RgbColorRegex();

    [GeneratedRegex(@"\b(aliceblue|antiquewhite|aqua|aquamarine|azure|beige|bisque|black|blanchedalmond|blue|blueviolet|brown|burlywood|cadetblue|chartreuse|chocolate|coral|cornflowerblue|cornsilk|crimson|cyan|darkblue|darkcyan|darkgoldenrod|darkgray|darkgreen|darkgrey|darkkhaki|darkmagenta|darkolivegreen|darkorange|darkorchid|darkred|darksalmon|darkseagreen|darkslateblue|darkslategray|darkslategrey|darkturquoise|darkviolet|deeppink|deepskyblue|dimgray|dimgrey|dodgerblue|firebrick|floralwhite|forestgreen|fuchsia|gainsboro|ghostwhite|gold|goldenrod|gray|green|greenyellow|grey|honeydew|hotpink|indianred|indigo|ivory|khaki|lavender|lavenderblush|lawngreen|lemonchiffon|lightblue|lightcoral|lightcyan|lightgoldenrodyellow|lightgray|lightgreen|lightgrey|lightpink|lightsalmon|lightseagreen|lightskyblue|lightslategray|lightslategrey|lightsteelblue|lightyellow|lime|limegreen|linen|magenta|maroon|mediumaquamarine|mediumblue|mediumorchid|mediumpurple|mediumseagreen|mediumslateblue|mediumspringgreen|mediumturquoise|mediumvioletred|midnightblue|mintcream|mistyrose|moccasin|navajowhite|navy|oldlace|olive|olivedrab|orange|orangered|orchid|palegoldenrod|palegreen|paleturquoise|palevioletred|papayawhip|peachpuff|peru|pink|plum|powderblue|purple|rebeccapurple|red|rosybrown|royalblue|saddlebrown|salmon|sandybrown|seagreen|seashell|sienna|silver|skyblue|slateblue|slategray|slategrey|snow|springgreen|steelblue|tan|teal|thistle|tomato|turquoise|violet|wheat|white|whitesmoke|yellow|yellowgreen)\b", RegexOptions.IgnoreCase)]
    private static partial Regex NamedColorRegex();

    [GeneratedRegex(@"#[a-fA-F0-9]{3,8}\b|rgba?\s*\([^)]+\)|hsla?\s*\([^)]+\)", RegexOptions.IgnoreCase)]
    private static partial Regex AllColorsRegex();

    [GeneratedRegex(@"#[a-zA-Z][\w-]*\b")]
    private static partial Regex IdSelectorRegex();

    [GeneratedRegex(@":\s*0(px|em|rem|%|pt|cm|mm|in|pc|ex|ch|vw|vh|vmin|vmax)(?![a-zA-Z0-9])")]
    private static partial Regex ZeroWithUnitRegex();

    [GeneratedRegex(@"-(webkit|moz|ms|o)-")]
    private static partial Regex VendorPrefixRegex();

    [GeneratedRegex(@"^\$[\w-]+\s*:", RegexOptions.Multiline)]
    private static partial Regex VariableDeclarationRegex();

    [GeneratedRegex(@"@mixin\s+[\w-]+", RegexOptions.Multiline)]
    private static partial Regex MixinDeclarationRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex MultipleWhitespaceRegex();
}
