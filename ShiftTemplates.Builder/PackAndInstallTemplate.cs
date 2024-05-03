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


        Process process = Process.Start("dotnet", $"pack {fullPath} --no-build --configuration Release --no-build --output {projectPath}/bin/packed");
        //wait for the above process to complete before writing to console
        process.WaitForExit(-1);

        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine("---------------------------------------------------------------------");
    }

    private void UnInstallTemplate()
    {
        Console.WriteLine("Uninstall the Template");
        Console.WriteLine();
        Console.WriteLine();

        Process process = Process.Start("dotnet", $"new uninstall ShiftSoftware.ShiftTemplates");
        //wait for the above process to complete before writing to console
        process.WaitForExit(-1);

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

        Process process = Process.Start("dotnet", $"new install {packagePath} --force");
        //wait for the above process to complete before writing to console
        process.WaitForExit(-1);

        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine("---------------------------------------------------------------------");
    }
}