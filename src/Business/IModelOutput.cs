namespace Eval.Business;

public interface IModelOutput : IDisposable
{
    Task GenerateOutputsAsync(string inputPromptsPath, string dataDirectoryPath);
}
