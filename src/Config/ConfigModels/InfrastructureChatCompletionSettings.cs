namespace Eval.Config.ConfigModels;

public sealed class InfrastructureChatCompletionSettings
{
    public ChatCompletionApiSettings ChatCompletionApi { get; set; } = new();
}

public sealed class ChatCompletionApiSettings
{
    public string Protocol { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 0;
}
