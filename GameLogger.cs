using System.Collections.Generic;
using System.IO;
using System.Text;
using Godot;

public static class GameLogger
{
    private static string logPath = "Train/game_results.txt";

    public static void WriteLog(List<string> results)
    {
        var fullPath = ProjectSettings.GlobalizePath(logPath);
        File.WriteAllLines(fullPath, results, Encoding.UTF8);
    }
}
