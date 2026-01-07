using System.Text.RegularExpressions;
using Eval.Infrastructure;
using Eval.Logging;

namespace Eval.Grading.Markdown;

public class MarkdownGrader : IMarkdownGrader
{    
    private readonly IEvalLogger _logger;
    private readonly double _validityWeight;
    private readonly double _varietyWeight;
    private double _lastGradeScore = 0.0;

    public MarkdownGrader(        
        double validityWeight,
        double varietyWeight,
        IEvalLogger logger)
    {        
        _validityWeight = validityWeight;
        _varietyWeight = varietyWeight;
        _logger = logger;
    }

    public double Grade()
    {
        return _lastGradeScore;
    }

    private void ValidateCodeBlocks(string content, MarkdownGraderResult validation)
    {
        var openCodeBlocks = Regex.Matches(content, @"```");
        if (openCodeBlocks.Count % 2 != 0)
        {
            validation.IsValid = false;
            validation.ValidationErrors.Add("Unclosed code block detected");
        }
    }

    private void ValidateLinks(string content, MatchCollection linkMatches, MarkdownGraderResult validation)
    {
        foreach (Match link in linkMatches)
        {
            if (string.IsNullOrWhiteSpace(link.Groups[1].Value) || string.IsNullOrWhiteSpace(link.Groups[2].Value))
            {
                validation.IsValid = false;
                validation.ValidationErrors.Add($"Invalid link format: {link.Value}");
            }
        }
    }

    private void ValidateBoldMarkers(string content, MarkdownGraderResult validation)
    {
        var boldDoubleAsterisk = Regex.Matches(content, @"\*\*");
        var boldDoubleUnderscore = Regex.Matches(content, @"__");
        if (boldDoubleAsterisk.Count % 2 != 0)
        {
            validation.IsValid = false;
            validation.ValidationErrors.Add("Unmatched bold marker (**)");
        }
        if (boldDoubleUnderscore.Count % 2 != 0)
        {
            validation.IsValid = false;
            validation.ValidationErrors.Add("Unmatched bold marker (__)");
        }
    }

    private void ValidateItalicMarkers(string content, MarkdownGraderResult validation)
    {
        var contentWithoutBold = Regex.Replace(content, @"\*\*|__", "");
        var italicAsterisk = Regex.Matches(contentWithoutBold, @"(?<!\*)\*(?!\*)");
        var italicUnderscore = Regex.Matches(contentWithoutBold, @"(?<!_)_(?!_)");
        if (italicAsterisk.Count % 2 != 0)
        {
            validation.IsValid = false;
            validation.ValidationErrors.Add("Unmatched italic marker (*)");
        }
        if (italicUnderscore.Count % 2 != 0)
        {
            validation.IsValid = false;
            validation.ValidationErrors.Add("Unmatched italic marker (_)");
        }
    }

    private void ValidateImages(MatchCollection imageMatches, MarkdownGraderResult validation)
    {
        foreach (Match image in imageMatches)
        {
            if (string.IsNullOrWhiteSpace(image.Groups[2].Value))
            {
                validation.IsValid = false;
                validation.ValidationErrors.Add($"Invalid image format (missing URL): {image.Value}");
            }
        }
    }

    private void ValidateTables(MatchCollection tableRows, MarkdownGraderResult validation)
    {
        if (tableRows.Count > 0)
        {
            int? expectedColumns = null;
            foreach (Match row in tableRows)
            {
                int columns = row.Value.Split('|').Length - 2;
                if (expectedColumns == null)
                {
                    expectedColumns = columns;
                }
                else if (columns != expectedColumns)
                {
                    validation.IsValid = false;
                    validation.ValidationErrors.Add($"Inconsistent table column count in row: {row.Value}");
                    break;
                }
            }
        }
    }

    private void ValidateReferenceLinkDefinitions(string content, MarkdownGraderResult validation)
    {
        var refLinkUsage = Regex.Matches(content, @"\[([^\]]+)\]\[([^\]]+)\]");
        var refLinkDefs = Regex.Matches(content, @"^\[([^\]]+)\]:\s*.+$", RegexOptions.Multiline);
        var definedRefs = new HashSet<string>(refLinkDefs.Cast<Match>().Select(m => m.Groups[1].Value.ToLower()));
        foreach (Match refLink in refLinkUsage)
        {
            string refId = refLink.Groups[2].Value.ToLower();
            if (!definedRefs.Contains(refId))
            {
                validation.IsValid = false;
                validation.ValidationErrors.Add($"Undefined reference link: [{refId}]");
            }
        }
    }

    private void ValidateHtmlTags(string content, MarkdownGraderResult validation)
    {
        var htmlTagMatches = Regex.Matches(content, @"<(/?)(\w+)(?:\s[^>]*)?>", RegexOptions.IgnoreCase);
        var tagStack = new Stack<string>();
        foreach (Match tag in htmlTagMatches)
        {
            string tagName = tag.Groups[2].Value.ToLower();
            bool isClosing = tag.Groups[1].Value == "/";
            
            if (new[] { "br", "hr", "img", "input", "meta", "link" }.Contains(tagName))
                continue;

            if (isClosing)
            {
                if (tagStack.Count == 0 || tagStack.Peek() != tagName)
                {
                    validation.IsValid = false;
                    validation.ValidationErrors.Add($"Mismatched HTML closing tag: </{tagName}>");
                }
                else
                {
                    tagStack.Pop();
                }
            }
            else
            {
                tagStack.Push(tagName);
            }
        }
        if (tagStack.Count > 0)
        {
            validation.IsValid = false;
            validation.ValidationErrors.Add($"Unclosed HTML tags: {string.Join(", ", tagStack)}");
        }
    }

    private void ValidateHeadingHierarchy(string content, MarkdownGraderResult validation)
    {
        var headings = Regex.Matches(content, @"^(#{1,6})\s", RegexOptions.Multiline);
        int? previousLevel = null;
        foreach (Match heading in headings)
        {
            int currentLevel = heading.Groups[1].Value.Length;
            if (previousLevel.HasValue && currentLevel - previousLevel.Value > 1)
            {
                validation.IsValid = false;
                validation.ValidationErrors.Add($"Skipped heading level from H{previousLevel.Value} to H{currentLevel}");
                break;
            }
            previousLevel = currentLevel;
        }
    }

    private int CountFeaturesUsed(MarkdownGraderResult validation, MatchCollection imageMatches, 
        MatchCollection tableRows, MatchCollection blockquotes, MatchCollection horizontalRules, 
        string content)
    {
        int featuresUsed = 0;
        
        if (validation.HasHeadings) featuresUsed++;
        if (validation.HasCodeBlocks) featuresUsed++;
        if (validation.HasLinks) featuresUsed++;
        if (validation.HasLists) featuresUsed++;
        if (imageMatches.Count > 0) featuresUsed++;
        if (tableRows.Count > 0) featuresUsed++;
        if (blockquotes.Count > 0) featuresUsed++;
        if (horizontalRules.Count > 0) featuresUsed++;
        
        var boldDoubleAsterisk = Regex.Matches(content, @"\*\*");
        var boldDoubleUnderscore = Regex.Matches(content, @"__");
        var contentWithoutBold = Regex.Replace(content, @"\*\*|__", "");
        var italicAsterisk = Regex.Matches(contentWithoutBold, @"(?<!\*)\*(?!\*)");
        var italicUnderscore = Regex.Matches(contentWithoutBold, @"(?<!_)_(?!_)");
        
        if (boldDoubleAsterisk.Count > 0 || boldDoubleUnderscore.Count > 0 || 
            italicAsterisk.Count > 0 || italicUnderscore.Count > 0)
        {
            featuresUsed++;
        }
        
        return featuresUsed;
    }

    async Task<(MarkdownGraderResult validation, double finalScore)> IMarkdownGrader.GradeAsync(
        string modelOutputContent)
    {
        return await Task.Run(() =>
        {
            var validation = new MarkdownGraderResult
            {
                IsValid = true
            };

            if (string.IsNullOrWhiteSpace(modelOutputContent))
            {
                validation.IsValid = false;
                validation.ValidationErrors.Add("Content is empty or whitespace");
                _lastGradeScore = 0.0;
                return (validation, 0.0);
            }

            // Check for headings
            var headingMatches = Regex.Matches(modelOutputContent, @"^#{1,6}\s+.+$", RegexOptions.Multiline);
            validation.HeadingCount = headingMatches.Count;
            validation.HasHeadings = validation.HeadingCount > 0;

            // Check for code blocks
            var codeBlockMatches = Regex.Matches(modelOutputContent, @"```[\s\S]*?```", RegexOptions.Multiline);
            validation.CodeBlockCount = codeBlockMatches.Count;
            validation.HasCodeBlocks = validation.CodeBlockCount > 0;

            // Check for links
            var linkMatches = Regex.Matches(modelOutputContent, @"\[([^\]]+)\]\(([^\)]+)\)");
            validation.LinkCount = linkMatches.Count;
            validation.HasLinks = validation.LinkCount > 0;

            // Check for lists (unordered and ordered)
            var listMatches = Regex.Matches(modelOutputContent, @"^[\*\-\+]\s+.+$|^\d+\.\s+.+$", RegexOptions.Multiline);
            validation.ListCount = listMatches.Count;
            validation.HasLists = validation.ListCount > 0;

            // Validate all markdown elements
            ValidateCodeBlocks(modelOutputContent, validation);
            ValidateLinks(modelOutputContent, linkMatches, validation);
            ValidateBoldMarkers(modelOutputContent, validation);
            ValidateItalicMarkers(modelOutputContent, validation);

            // Validate images
            var imageMatches = Regex.Matches(modelOutputContent, @"!\[([^\]]*)\]\(([^\)]+)\)");
            ValidateImages(imageMatches, validation);

            // Validate tables
            var tableRows = Regex.Matches(modelOutputContent, @"^\|.+\|$", RegexOptions.Multiline);
            ValidateTables(tableRows, validation);

            ValidateReferenceLinkDefinitions(modelOutputContent, validation);
            ValidateHtmlTags(modelOutputContent, validation);
            ValidateHeadingHierarchy(modelOutputContent, validation);

            // Check additional features
            var blockquotes = Regex.Matches(modelOutputContent, @"^>\s*.+$", RegexOptions.Multiline);
            var horizontalRules = Regex.Matches(modelOutputContent, @"^([-*_])\1{2,}\s*$", RegexOptions.Multiline);

            // Calculate variety score
            int featuresUsed = CountFeaturesUsed(validation, imageMatches, tableRows, blockquotes, 
                horizontalRules, modelOutputContent);
            int totalFeatures = 9;

            double varietyScore = (double)featuresUsed / totalFeatures;
            
            // Calculate final score using configured weights
            double validityScore = validation.IsValid ? 1.0 : 0.0;
            double finalScore = (validityScore * _validityWeight) + (varietyScore * _varietyWeight);

            _lastGradeScore = finalScore;

            var logMessage = $@"Markdown validation results:
  Valid: {validation.IsValid}
  Features Used: {featuresUsed}/{totalFeatures}
  Headings: {validation.HeadingCount}
  Code Blocks: {validation.CodeBlockCount}
  Links: {validation.LinkCount}
  Lists: {validation.ListCount}
  Images: {imageMatches.Count}
  Tables: {tableRows.Count}
  Blockquotes: {blockquotes.Count}
  Horizontal Rules: {horizontalRules.Count}";

            if (validation.ValidationErrors.Any())
            {
                logMessage += $"\n  Errors: {string.Join(", ", validation.ValidationErrors)}";
            }
            
            logMessage += $@"
  Validity Score: {validityScore:F2} ({_validityWeight:P0} weight)
  Variety Score: {varietyScore:F2} ({_varietyWeight:P0} weight)
Final Markdown Grade Score: {finalScore:F2}";

            _logger.LogVerbose(logMessage, ConsoleColor.Blue);

            return (validation, finalScore);
        });
    }
}
