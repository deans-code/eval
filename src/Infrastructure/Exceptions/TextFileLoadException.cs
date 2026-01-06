namespace Eval.Infrastructure.Exceptions;

/// <summary>
/// Exception thrown when a text file fails to load.
/// </summary>
public sealed class TextFileLoadException : Exception
{
    public string FilePath { get; }

    public TextFileLoadException(string filePath, Exception innerException)
        : base($"Failed to load text file: {filePath}", innerException)
    {
        FilePath = filePath;
    }
}
