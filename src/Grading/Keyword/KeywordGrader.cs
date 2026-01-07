using System.Text.RegularExpressions;
using Eval.Infrastructure;
using Eval.Logging;
using Eval.Config.ConfigModels;

namespace Eval.Grading.Keyword;

public class KeywordGrader : IKeywordGrader
{
    private readonly IEvalFileAccess _evalFileAccess;
    private readonly IEvalLogger _logger;
    private readonly List<ExpectedKeyword> _expectedKeywords;
    private double _lastGradeScore = 0.0;

    public KeywordGrader(
        IEvalFileAccess evalFileAccess,
        IEvalLogger logger,
        List<ExpectedKeyword> expectedKeywords)
    {
        _evalFileAccess = evalFileAccess;
        _logger = logger;
        _expectedKeywords = expectedKeywords;
    }

    public double Grade()
    {
        return _lastGradeScore;
    }

    async Task<(KeywordGraderResult analysis, double finalScore)> IKeywordGrader.GradeAsync(
        string modelOutputContent)
    {
        return await Task.Run(() =>
        {
            var analysis = new KeywordGraderResult();

            if (string.IsNullOrWhiteSpace(modelOutputContent))
            {
                _lastGradeScore = 0.0;
                return (analysis, 0.0);
            }

            if (_expectedKeywords == null || _expectedKeywords.Count == 0)
            {
                _lastGradeScore = 1.0;
                return (analysis, 1.0);
            }

            analysis.TotalKeywords = _expectedKeywords.Count;

            foreach (ExpectedKeyword expectedKeyword in _expectedKeywords)
            {
                // Case-insensitive keyword matching using word boundaries
                string pattern = $@"\b{Regex.Escape(expectedKeyword.Keyword)}\b";
                MatchCollection matches = Regex.Matches(modelOutputContent, pattern, RegexOptions.IgnoreCase);

                var result = new KeywordGraderResultItem
                {
                    Keyword = expectedKeyword.Keyword,
                    ExpectedCount = expectedKeyword.MinimumOccurrences,
                    ActualCount = matches.Count,
                    Met = matches.Count >= expectedKeyword.MinimumOccurrences
                };

                analysis.Results[expectedKeyword.Keyword] = result;

                if (result.Met)
                {
                    analysis.TotalKeywordsMet++;
                }
            }

            // Calculate score as percentage of keywords that met their minimum occurrence
            double finalScore = analysis.TotalKeywords > 0 
                ? (double)analysis.TotalKeywordsMet / analysis.TotalKeywords 
                : 1.0;

            _lastGradeScore = finalScore;

            var logMessage = $"Keyword analysis results:\n  Keywords Met: {analysis.TotalKeywordsMet}/{analysis.TotalKeywords}\n";
            
            foreach (KeywordGraderResultItem? result in analysis.Results.Values.OrderBy(r => r.Keyword))
            {
                string status = result.Met ? "Pass" : "Fail";
                logMessage += $"  {status} - {result.Keyword}: {result.ActualCount} (minimum: {result.ExpectedCount})\n";
            }
            
            logMessage += $"Final Keyword Grade Score: {finalScore:F2}";
            
            _logger.LogVerbose(logMessage, ConsoleColor.Yellow);

            return (analysis, finalScore);
        });
    }
}
