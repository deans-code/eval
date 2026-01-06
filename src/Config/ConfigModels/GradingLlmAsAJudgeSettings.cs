namespace Eval.Config.ConfigModels;

public sealed class GradingLlmAsAJudgeSettings
{
    public string SystemPromptPath { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string ExampleOutputsPath { get; set; } = string.Empty;
}
