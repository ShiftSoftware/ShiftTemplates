using System.Diagnostics;

namespace ShiftTemplates.Builder;

public  class CreateProject
{
    public CreateProject Create(bool includeSampleApp, string identityType, bool addTest = false, bool addFunctions = false, bool launch = false)
    {
        Console.WriteLine();
        Console.WriteLine("---------------------------------------------------------------------");

        Console.WriteLine("Create Project using ShiftSoftware.ShiftTemplates");
        Console.WriteLine();
        Console.WriteLine();

        var path = @$"{Tools.GetProjectPath()}\TestProject";

        var fullPath = System.IO.Path.GetFullPath(path);

        if (Directory.Exists(fullPath))
            Directory.Delete(fullPath, true);
        
        Process process = Process.Start("dotnet", $"new shift --includeSampleApp {includeSampleApp} --shiftIdentityHostingType {identityType} --addTest {addTest} --addFunctions {addFunctions} -n Test --output {fullPath}");
        //wait for the above process to complete before writing to console
        process.WaitForExit(-1);

        if (launch)
        {
            //open the solution in Visual Studio
            var launchProcess = Process.Start("cmd", $"/c start {fullPath}/Test.sln");

            launchProcess.WaitForExit(-1);
        }


        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine("---------------------------------------------------------------------");

        return this;
    }
}