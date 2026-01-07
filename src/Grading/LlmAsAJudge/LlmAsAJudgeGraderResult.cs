namespace Eval.Grading.LlmAsAJudge;

public sealed class LlmAsAJudgeGraderResult
{
    public bool IsValid { get; set; } = true;
    public List<string> Feedback { get; set; } = new();
    public double Accuracy { get; set; }
    public double Language { get; set; }
    public double Conciseness { get; set; }
    public double Clarity { get; set; }
}
