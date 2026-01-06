namespace Eval.Infrastructure.Exceptions;

/// <summary>
/// Exception thrown when the model output directory is not found.
/// </summary>
public sealed class ModelOutputDirectoryNotFoundException : Exception
{
    public string DirectoryPath { get; }

    public ModelOutputDirectoryNotFoundException(string directoryPath)
        : base($"Model output directory not found: {directoryPath}")
    {
        DirectoryPath = directoryPath;
    }
}
