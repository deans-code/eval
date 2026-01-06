namespace Eval.Grading.LlmAsAJudge;

public interface ILlmAsAJudgeGrader
{
    Task<(LlmJudgmentResult result, double finalScore)> GradeAsync(
        string modelOutputContent,
        string inputPromptContent,
        string modelOutputSystemPromptContent,
        IEnumerable<string> sampleOutputPaths);
}
