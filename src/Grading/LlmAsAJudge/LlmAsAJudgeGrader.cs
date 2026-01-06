using Eval.Domain;
using Eval.Infrastructure;
using Eval.Logging;

namespace Eval.Grading.LlmAsAJudge;

public sealed class LlmAsAJudgeGrader : ILlmAsAJudgeGrader
{
    private readonly EvalFileAccess _evalFileAccess;
    private readonly IChatCompletionGeneration _chatCompletionGeneration;
    private readonly IEvalLogger _logger;
    private readonly string _systemPromptPath;
    private readonly string _model;

    public LlmAsAJudgeGrader(
        EvalFileAccess evalFileAccess,
        IChatCompletionGeneration chatCompletionGeneration,
        string systemPromptPath,
        string model,
        IEvalLogger logger)
    {
        _evalFileAccess = evalFileAccess;
        _chatCompletionGeneration = chatCompletionGeneration;
        _systemPromptPath = systemPromptPath;
        _model = model;
        _logger = logger;
    }

    public async Task<(LlmJudgmentResult result, double finalScore)> GradeAsync(
        string modelOutputContent,
        string inputPromptContent,
        string modelOutputSystemPromptContent,
        IEnumerable<string> sampleOutputPaths)
    {
        var result = new LlmJudgmentResult();
        double finalScore = 0.0;

        string systemPrompt = await _evalFileAccess.LoadTextFileAsync(_systemPromptPath);

        string[] examples = await LoadFileContents(sampleOutputPaths);
        string exampleHighQualityOutputs = string.Join("\n\n", examples);

        string prompt = systemPrompt
            .Replace("{system_prompt}", modelOutputSystemPromptContent)
            .Replace("{user_prompt}", inputPromptContent)
            .Replace("{example_high_quality_outputs}", exampleHighQualityOutputs)
            .Replace("{output_under_evaluation}", modelOutputContent);

        AugmentedChatCompletion response = await _chatCompletionGeneration.GetChatCompletionAsync(
            "Run evaluation.",
            prompt,
            _model);

        result.Feedback.Add(response.ExtractedMessage);

        // Parse response.Text as JSON and extract scores
        try
        {
            var scores = System.Text.Json.JsonDocument.Parse(response.ExtractedMessage).RootElement;
            result.Accuracy = scores.GetProperty("Accuracy").GetDouble();
            result.Language = scores.GetProperty("Language").GetDouble();
            result.Conciseness = scores.GetProperty("Conciseness").GetDouble();
            result.Clarity = scores.GetProperty("Clarity").GetDouble();
            finalScore = (result.Accuracy + result.Language + result.Conciseness + result.Clarity) / 4.0;

            var logMessage = $@"LLM-as-a-judge validation results:
  Accuracy: {result.Accuracy}
  Language: {result.Language}
  Conciseness: {result.Conciseness}
  Clarity: {result.Clarity}
Final LLM-as-a-judge Grade Score: {finalScore:F2}";

            _logger.LogVerbose(logMessage, ConsoleColor.Cyan);

        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to parse LLM judge response: {ex.Message}");
            result.IsValid = false;
            finalScore = 0.0;
        }

        return (result, finalScore);
    }

    private async Task<string[]> LoadFileContents(IEnumerable<string> sampleOutputPaths)
    {
        IEnumerable<Task<string>> exampleTasks = sampleOutputPaths.Select(async path =>
        {
            string content = await _evalFileAccess.LoadTextFileAsync(path);
            return $"<example>\n{content}\n</example>";
        });

        string[] examples = await Task.WhenAll(exampleTasks);

        return examples;
    }
}
