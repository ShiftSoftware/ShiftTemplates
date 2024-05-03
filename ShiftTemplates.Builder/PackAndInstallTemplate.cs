using System.Diagnostics;

namespace ShiftTemplates.Builder;

public class PackAndInstallTemplate
{
    public void PackAndInstall()
    {
        this.Pack();

        this.UnInstallTemplate();

        this.InstallTemplate();
    }

    private void Pack()
    {
        Console.WriteLine();
        Console.WriteLine("---------------------------------------------------------------------");

        var tempalteProjectPath = "../../../../ShiftTemplates.csproj";

        var fullPath = System.IO.Path.GetFullPath(tempalteProjectPath);


        Console.WriteLine("Installing the Template");
        Console.WriteLine();
        Console.WriteLine();


        Process process = Process.Start("dotnet", $"pack {fullPath} --no-build --configuration Release --output ../../../bin/packed");
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

        var packagePath = System.IO.Directory.GetFiles("../../../bin/packed").FirstOrDefault();

        Process process = Process.Start("dotnet", $"new uninstall ShiftSoftware.ShiftTemplates");
        //wait for the above process to complete before writing to console
        process.WaitForExit(-1);

        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine("---------------------------------------------------------------------");
    }

    private void InstallTemplate()
    {
        Console.WriteLine("Installing the Template");
        Console.WriteLine();
        Console.WriteLine();


        var packagePath = System.IO.Directory.GetFiles("../../../bin/packed").FirstOrDefault();

        Process process = Process.Start("dotnet", $"new install {packagePath} --force");
        //wait for the above process to complete before writing to console
        process.WaitForExit(-1);

        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine("---------------------------------------------------------------------");
    }
}