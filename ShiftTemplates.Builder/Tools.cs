using System.Reflection;

namespace ShiftTemplates.Builder;

public class Tools
{
    public static string GetProjectPath()
    {
        var baseDirectory = AppContext.BaseDirectory;

        Console.WriteLine();
        Console.WriteLine();
        Console.Write($"Base Directory is: {baseDirectory}");
        Console.WriteLine();
        Console.WriteLine();

        var projectPath = baseDirectory.Substring(0, baseDirectory.IndexOf("ShiftTemplates.Builder"));

        return projectPath;
    }
}