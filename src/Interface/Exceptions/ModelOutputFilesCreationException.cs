namespace Eval.Interface.Exceptions;

/// <summary>
/// Exception thrown when model output files creation fails.
/// </summary>
public sealed class ModelOutputFilesCreationException : Exception
{
    public ModelOutputFilesCreationException(string message, Exception innerException)
        : base($"Failed to create model output files: {message}", innerException)
    {
    }

    public ModelOutputFilesCreationException(string message)
        : base($"Failed to create model output files: {message}")
    {
    }
}
