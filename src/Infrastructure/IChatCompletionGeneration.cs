using Eval.Domain;

namespace Eval.Infrastructure;

public interface IChatCompletionGeneration : IDisposable
{
    Task<AugmentedChatCompletion> GetChatCompletionAsync(string inputText, string? systemPrompt, string model);
}
