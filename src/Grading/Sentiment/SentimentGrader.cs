using System.Text.Json;
using Eval.Domain;
using Eval.Infrastructure;
using Eval.Logging;

namespace Eval.Grading.Sentiment;

public class SentimentGrader : ISentimentGrader
{
    private readonly EvalFileAccess _evalFileAccess;
    private readonly IChatCompletionGeneration _chatCompletionGeneration;
    private readonly string _systemPromptPath;
    private readonly SentimentGraderResult _targetScores;
    private readonly string _model;
    private double _lastGradeScore = 0.0;
    private readonly IEvalLogger _logger;

    public SentimentGrader(
        EvalFileAccess evalFileAccess,
        IChatCompletionGeneration chatCompletionGeneration, 
        string systemPromptPath,
        SentimentGraderResult targetScores,
        string model,
        IEvalLogger logger)    
    {
        _evalFileAccess = evalFileAccess;
        _chatCompletionGeneration = chatCompletionGeneration;
        _systemPromptPath = systemPromptPath;
        _targetScores = targetScores;
        _model = model;
        _logger = logger;
    }

    public double Grade()
    {
        return _lastGradeScore;
    }

    async Task<(SentimentGraderResult scores, double finalScore)> ISentimentGrader.GradeAsync(
        string modelOutputContent)
    {
        string systemPrompt = await _evalFileAccess.LoadTextFileAsync(_systemPromptPath);

        AugmentedChatCompletion completion = await _chatCompletionGeneration.GetChatCompletionAsync(
            modelOutputContent,
            systemPrompt,
            _model);

        string jsonResponse = completion.ChatCompletion?.Choices.FirstOrDefault()?.Message.Content ?? string.Empty;

        if (string.IsNullOrWhiteSpace(jsonResponse))
        {
            throw new InvalidOperationException("Empty response from chat completion");
        }

        SentimentGraderResult? scores = JsonSerializer.Deserialize<SentimentGraderResult>(jsonResponse);

        if (scores == null)
        {
            throw new InvalidOperationException("Failed to deserialize sentiment scores");
        }

        // Calculate individual emotion scores based on difference from target
        double angerScore = 1.0 - Math.Abs(_targetScores.Anger - scores.Anger);
        double fearScore = 1.0 - Math.Abs(_targetScores.Fear - scores.Fear);
        double anticipationScore = 1.0 - Math.Abs(_targetScores.Anticipation - scores.Anticipation);
        double trustScore = 1.0 - Math.Abs(_targetScores.Trust - scores.Trust);
        double surpriseScore = 1.0 - Math.Abs(_targetScores.Surprise - scores.Surprise);
        double sadnessScore = 1.0 - Math.Abs(_targetScores.Sadness - scores.Sadness);
        double joyScore = 1.0 - Math.Abs(_targetScores.Joy - scores.Joy);
        double disgustScore = 1.0 - Math.Abs(_targetScores.Disgust - scores.Disgust);

        // Average all emotion scores to get final score
        double finalScore = (angerScore + fearScore + anticipationScore + trustScore + 
                           surpriseScore + sadnessScore + joyScore + disgustScore) / 8.0;

        _lastGradeScore = finalScore;

        var logMessage = $@"Sentiment scores:
  Anger: {scores.Anger:F2} (Target: {_targetScores.Anger:F2})
  Fear: {scores.Fear:F2} (Target: {_targetScores.Fear:F2})
  Anticipation: {scores.Anticipation:F2} (Target: {_targetScores.Anticipation:F2})
  Trust: {scores.Trust:F2} (Target: {_targetScores.Trust:F2})
  Surprise: {scores.Surprise:F2} (Target: {_targetScores.Surprise:F2})
  Sadness: {scores.Sadness:F2} (Target: {_targetScores.Sadness:F2})
  Joy: {scores.Joy:F2} (Target: {_targetScores.Joy:F2})
  Disgust: {scores.Disgust:F2} (Target: {_targetScores.Disgust:F2})
Final Sentiment Grade Score: {finalScore:F2}";

        _logger.LogVerbose(logMessage, ConsoleColor.Green);  

        return (scores, finalScore);
    }
}
