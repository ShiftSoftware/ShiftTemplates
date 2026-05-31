using System.Diagnostics;

namespace ShiftTemplates.Builder;

public class PackAndInstallTemplate
{
    public void PackAndInstall()
    {
        var projectPath = Tools.GetProjectPath();

        this.Pack(projectPath);

        this.UnInstallTemplate();

        this.InstallTemplate(projectPath);
    }

    private void Pack(string projectPath)
    {
        Console.WriteLine();
        Console.WriteLine("---------------------------------------------------------------------");

        var fullPath = System.IO.Path.GetFullPath($"{projectPath}/ShiftTemplates.csproj");

        Console.WriteLine("Installing the Template");
        Console.WriteLine();
        Console.WriteLine();

        if (Directory.Exists($"{projectPath}/bin/packed"))
            Directory.Delete($"{projectPath}/bin/packed", true);

        using var process = Process.Start("dotnet", $"pack {fullPath} --no-build --configuration Release --no-build --output {projectPath}/bin/packed");
        process!.WaitForExit(-1);

        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine("---------------------------------------------------------------------");
    }

    private void UnInstallTemplate()
    {
        Console.WriteLine("Uninstall the Template");
        Console.WriteLine();
        Console.WriteLine();

        using var process = Process.Start("dotnet", $"new uninstall ShiftSoftware.ShiftTemplates");
        process!.WaitForExit(-1);

        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine("---------------------------------------------------------------------");
    }

    private void InstallTemplate(string projectPath)
    {
        Console.WriteLine("Installing the Template");
        Console.WriteLine();
        Console.WriteLine();

        var packagePath = System.IO.Directory.GetFiles($"{projectPath}/bin/packed").FirstOrDefault();

        using var process = Process.Start("dotnet", $"new install {packagePath} --force");
        process!.WaitForExit(-1);

        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine("---------------------------------------------------------------------");
    }
}