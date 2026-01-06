namespace Eval.Config.ConfigModels;

public sealed class BusinessOutputGradingSettings
{
    public double SentimentWeight { get; set; }
    public double MarkdownWeight { get; set; }
    public double KeywordWeight { get; set; }
    public double LlmAsAJudgeScoreWeight { get; set; }
}
