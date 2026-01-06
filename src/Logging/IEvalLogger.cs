namespace Eval.Logging;

public interface IEvalLogger
{
    void Log(string message);
    void Log(string message, ConsoleColor color);
    void LogInfo(string message, ConsoleColor? color = null);
    void LogSuccess(string message, ConsoleColor? color = null);
    void LogWarning(string message, ConsoleColor? color = null);
    void LogError(string message, ConsoleColor? color = null);
    void LogDebug(string message, ConsoleColor? color = null);
    void LogProcess(string message, ConsoleColor? color = null);
    void LogVerbose(string message, ConsoleColor? color = null);
}
