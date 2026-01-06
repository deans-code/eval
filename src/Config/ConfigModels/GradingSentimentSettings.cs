namespace Eval.Config.ConfigModels;

public sealed class GradingSentimentSettings
{
    public string SystemPrompt { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public TargetSentimentScoresSettings TargetSentimentScores { get; set; } = new();
}

public sealed class TargetSentimentScoresSettings
{
    public double Anger { get; set; }
    public double Fear { get; set; }
    public double Anticipation { get; set; }
    public double Trust { get; set; }
    public double Surprise { get; set; }
    public double Sadness { get; set; }
    public double Joy { get; set; }
    public double Disgust { get; set; }
}
