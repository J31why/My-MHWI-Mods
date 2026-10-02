using SharpPluginLoader.Core;

namespace Common;

internal static class Logging
{
    public static string Name { get; set; } = string.Empty;
    public static void Info(string message)
    {
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}][{Name}] {message}");
        Log.Info($"[{Name}] {message}");
    }
    public static void Error(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Log.Error($"[{Name}] {message}");
    }

    public static void Warn(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}][{Name}] {message}");
        Log.Warn($"[{Name}] {message}");
    }
}
