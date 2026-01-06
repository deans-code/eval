namespace Eval.Logging;

public class EvalLogger : IEvalLogger
{
    public void Log(string message)
    {
        Console.WriteLine(message);
    }

    public void Log(string message, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    public void LogInfo(string message, ConsoleColor? color = null)
    {
        Log($"[INFO] {message}", color ?? ConsoleColor.Cyan);
    }

    public void LogSuccess(string message, ConsoleColor? color = null)
    {
        Log($"[SUCCESS] {message}", color ?? ConsoleColor.Green);
    }

    public void LogWarning(string message, ConsoleColor? color = null)
    {
        Log($"[WARNING] {message}", color ?? ConsoleColor.Yellow);
    }

    public void LogError(string message, ConsoleColor? color = null)
    {
        Log($"[ERROR] {message}", color ?? ConsoleColor.Red);
    }

    public void LogDebug(string message, ConsoleColor? color = null)
    {
        Log($"[DEBUG] {message}", color ?? ConsoleColor.Gray);
    }

    public void LogProcess(string message, ConsoleColor? color = null)
    {
        Log($"[PROCESS] {message}", color ?? ConsoleColor.Magenta);
    }

    public void LogVerbose(string message, ConsoleColor? color = null)
    {
        Log($"[VERBOSE] {message}", color ?? ConsoleColor.DarkCyan);
    }
}
