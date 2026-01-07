using Eval.Infrastructure.Exceptions;

namespace Eval.Infrastructure;

public sealed class EvalFileAccess : IEvalFileAccess
{    
    public async Task<Dictionary<string, string>> LoadAllTextFilesAsync(string dataDirectoryPath)
    {
        var result = new Dictionary<string, string>();

        string fullPath = Path.Combine(Directory.GetCurrentDirectory(), dataDirectoryPath);

        if (!Directory.Exists(fullPath))
        {
            throw new ModelOutputDirectoryNotFoundException(fullPath);
        }

        string[] textFiles = Directory.GetFiles(fullPath, "*.txt");

        foreach (var filePath in textFiles)
        {
            try
            {
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                string content = await File.ReadAllTextAsync(filePath);
                result[fileName] = content;
            }
            catch (Exception ex)
            {
                throw new MarkdownFileLoadException(filePath, ex);
            }
        }

        return result;
    }

    public async Task<string> LoadTextFileAsync(string filePath)
    {
        string fullPath = Path.Combine(Directory.GetCurrentDirectory(), filePath);

        if (!File.Exists(fullPath))
        {
            throw new TextFileLoadException(fullPath, new FileNotFoundException($"File not found: {fullPath}"));
        }

        try
        {
            return await File.ReadAllTextAsync(fullPath);
        }
        catch (Exception ex)
        {
            throw new TextFileLoadException(fullPath, ex);
        }
    }

    public async Task SaveMarkdownFileAsync(string dataDirectoryPath, string content, string fileNameTitle)
    {
        string fullPath = Path.Combine(Directory.GetCurrentDirectory(), dataDirectoryPath);

        if (!Directory.Exists(fullPath))
        {
            Directory.CreateDirectory(fullPath);
        }

        string filePath = Path.Combine(fullPath, $"{fileNameTitle}.md");

        try
        {
            await File.WriteAllTextAsync(filePath, content);
        }
        catch (Exception ex)
        {
            throw new MarkdownFileSaveException(filePath, ex);
        }
    }
}
