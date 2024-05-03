using System.Diagnostics;

namespace ShiftTemplates.Builder;

public  class CreateProject
{
    public void Create(bool includeSampleApp, string identityType, bool addTest = false, bool addFunctions = false)
    {
        Console.WriteLine();
        Console.WriteLine("---------------------------------------------------------------------");

        Console.WriteLine("Create Project using ShiftSoftware.ShiftTemplates");
        Console.WriteLine();
        Console.WriteLine();

        var path = "../../../../TestProject";

        var fullPath = System.IO.Path.GetFullPath(path);

        if (Directory.Exists(fullPath))
            Directory.Delete(fullPath, true);
        
        Process process = Process.Start("dotnet", $"new shift --includeSampleApp {includeSampleApp} --shiftIdentityHostingType {identityType} --addTest {addTest} --addFunctions {addFunctions} -n Test --output {fullPath}");
        //wait for the above process to complete before writing to console
        process.WaitForExit(-1);
         
        //open the solution in Visual Studio
        Process.Start("cmd", $"/c start {fullPath}/Test.sln");


        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine("---------------------------------------------------------------------");
    }
}