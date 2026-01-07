namespace Eval.Infrastructure;

public interface IEvalFileAccess
{
    Task<Dictionary<string, string>> LoadAllTextFilesAsync(string dataDirectoryPath);
    Task<string> LoadTextFileAsync(string filePath);
    Task SaveMarkdownFileAsync(string dataDirectoryPath, string content, string fileNameTitle);
}
