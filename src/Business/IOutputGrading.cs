namespace Eval.Business;

public interface IOutputGrading : IDisposable
{
    Task PerformGradingAsync(
        string modelOutputFilePath, 
        string inputPromptFilePath,
        string modelOutputSystemPrompt,
        IEnumerable<string> sampleOutputPaths);
}
