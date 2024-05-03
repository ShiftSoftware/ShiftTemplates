using System.Reflection;

namespace ShiftTemplates.Builder;

public class Tools
{
    public static string GetProjectPath()
    {
        var assembly = Assembly.GetExecutingAssembly().Location;
        
        var projectPath = assembly.Substring(0, assembly.IndexOf("\\ShiftTemplates.Builder"));

        return projectPath;
    }
}