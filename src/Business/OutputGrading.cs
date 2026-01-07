using Eval.Grading.Sentiment;
using Eval.Grading.Markdown;
using Eval.Grading.Keyword;
using Eval.Grading.LlmAsAJudge;
using Eval.Infrastructure;
using Eval.Logging;

namespace Eval.Business;

public sealed class OutputGrading : IOutputGrading
{    
    private readonly IEvalFileAccess _evalFileAccess;
    private readonly IChatCompletionGeneration _chatCompletionGeneration;
    private readonly IEvalLogger _logger;
    private readonly ISentimentGrader _sentimentGrader;
    private readonly IMarkdownGrader _markdownGrader;
    private readonly IKeywordGrader _keywordGrader;
    private readonly ILlmAsAJudgeGrader _llmAsAJudgeGrader;
    private readonly double _sentimentWeight;
    private readonly double _markdownWeight;
    private readonly double _keywordWeight;
    private readonly double _llmAsAJudgeScoreWeight;
    private bool _disposed = false;

    public OutputGrading(
        IEvalFileAccess evalFileAccess,
        IChatCompletionGeneration chatCompletionGeneration,
        ISentimentGrader sentimentGrader,
        IMarkdownGrader markdownGrader,
        IKeywordGrader keywordGrader,
        ILlmAsAJudgeGrader llmAsAJudgeGrader,
        double sentimentWeight,
        double markdownWeight,
        double keywordWeight,
        double llmAsAJudgeScoreWeight,
        IEvalLogger logger)    
    {
        _evalFileAccess = evalFileAccess;
        _chatCompletionGeneration = chatCompletionGeneration;
        _sentimentGrader = sentimentGrader;
        _markdownGrader = markdownGrader;
        _keywordGrader = keywordGrader;
        _llmAsAJudgeGrader = llmAsAJudgeGrader;
        _sentimentWeight = sentimentWeight;
        _markdownWeight = markdownWeight;
        _keywordWeight = keywordWeight;
        _llmAsAJudgeScoreWeight = llmAsAJudgeScoreWeight;
        _logger = logger;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    private void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {            
            _chatCompletionGeneration?.Dispose();            
        }
        
        _disposed = true;        
    }

    ~OutputGrading()
    {
        Dispose(false);
    }

    async Task IOutputGrading.PerformGradingAsync(
        string modelOutputFilePath, 
        string inputPromptFilePath,
        string modelOutputSystemPrompt,
        IEnumerable<string> sampleOutputPaths)
    {    
        _logger.LogProcess($"Grading file: {modelOutputFilePath}");

        _logger.LogProcess($"Processing: Grading output...");

        string modelOutputContent = await _evalFileAccess.LoadTextFileAsync(modelOutputFilePath);
        string inputPromptContent = await _evalFileAccess.LoadTextFileAsync(inputPromptFilePath);
        string modelOutputSystemPromptContent = await _evalFileAccess.LoadTextFileAsync(modelOutputSystemPrompt);
        
        if (!string.IsNullOrWhiteSpace(modelOutputContent))
        {
            try
            {                
                var sentimentTask = _sentimentGrader.GradeAsync(modelOutputContent);
                var markdownTask = _markdownGrader.GradeAsync(modelOutputContent);
                var keywordTask = _keywordGrader.GradeAsync(modelOutputContent);
                var llmAsAJudgeTask = _llmAsAJudgeGrader.GradeAsync(
                    modelOutputContent, 
                    inputPromptContent,
                    modelOutputSystemPromptContent,
                    sampleOutputPaths);

                await Task.WhenAll(sentimentTask, markdownTask, keywordTask, llmAsAJudgeTask);

                (SentimentGraderResult? sentimentScores, double sentimentScore) = await sentimentTask;
                (MarkdownGraderResult? markdownValidation, double markdownScore) = await markdownTask;
                (KeywordGraderResult? keywordAnalysis, double keywordScore) = await keywordTask;
                (LlmAsAJudgeGraderResult? llmJudgmentResult, double llmAsAJudgeScore) = await llmAsAJudgeTask;
                                
                double finalScore = (sentimentScore * _sentimentWeight)
                    + (markdownScore * _markdownWeight)
                    + (keywordScore * _keywordWeight)
                    + (llmAsAJudgeScore * _llmAsAJudgeScoreWeight);

                string fileName = Path.GetFileNameWithoutExtension(modelOutputFilePath);

                _logger.LogProcess($"Sentiment Score: {sentimentScore * 100:F2}% (Weight: {_sentimentWeight:P0})");
                _logger.LogProcess($"Markdown Score: {markdownScore * 100:F2}% (Weight: {_markdownWeight:P0})");
                _logger.LogProcess($"Keyword Score: {keywordScore * 100:F2}% (Weight: {_keywordWeight:P0})");
                _logger.LogProcess($"LLM-as-a-Judge Score: {llmAsAJudgeScore * 100:F2}% (Weight: {_llmAsAJudgeScoreWeight:P0})");
                _logger.LogProcess($"Completed grading: {fileName} - Final Score: {finalScore * 100:F2}%");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to grade {Path.GetFileName(modelOutputFilePath)}: {ex.Message}");
            }
        }
    }
}
