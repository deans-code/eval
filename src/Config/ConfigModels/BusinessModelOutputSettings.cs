namespace Eval.Config.ConfigModels;

public sealed class BusinessModelOutputSettings
{
    public string ModelOutputPath { get; set; } = string.Empty;
    public string InputPromptsPath { get; set; } = string.Empty;
    public string SystemPrompt { get; set; } = string.Empty;
    public string LargeLanguageModel { get; set; } = string.Empty;
}
