using System.Diagnostics;

namespace ShiftTemplates.Builder;

public class PackAndInstallTemplate
{
    public void PackAndInstall()
    {
        Console.WriteLine();
        Console.WriteLine("---------------------------------------------------------------------");

        var tempalteProjectPath = "../../../../ShiftTemplates.csproj";

        var fullPath = System.IO.Path.GetFullPath(tempalteProjectPath);


        Console.WriteLine("Installing the Template");
        Console.WriteLine();
        Console.WriteLine();


        Process packProcess = Process.Start("dotnet", $"pack {fullPath} --no-build --configuration Release --output ../../../bin/packed");
        //wait for the above process to complete before writing to console
        packProcess.WaitForExit(-1);

        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine("---------------------------------------------------------------------");



        Console.WriteLine("Installing the Template");
        Console.WriteLine();
        Console.WriteLine();


        var packagePath = System.IO.Directory.GetFiles("../../../bin/packed").FirstOrDefault();

        Process installProcess = Process.Start("dotnet", $"new install {packagePath} --force");
        //wait for the above process to complete before writing to console
        packProcess.WaitForExit(-1);

        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine("---------------------------------------------------------------------");

        //Process p = Process.Start("dotnet", $"pack {fullPath} --no-build --configuration Release --output ../../../bin/packed");
    }
}