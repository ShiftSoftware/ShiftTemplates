using System.Diagnostics;

namespace ShiftTemplates.Builder;

public class CreateShiftEntity
{
    public void Create()
    {
        Console.WriteLine();
        Console.WriteLine("---------------------------------------------------------------------");

        Console.WriteLine("Create a Shift Entity");
        Console.WriteLine();
        Console.WriteLine();

        var path = @$"{Tools.GetProjectPath()}/TestProject";

        var project = Path.GetFullPath($"{path}/Test.sln");

        Process process = Process.Start("dotnet", $"new shiftentity --project {project} --output {path} --solution Test --name ToDo");
        //wait for the above process to complete before writing to console
        process.WaitForExit(-1);


        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine("---------------------------------------------------------------------");
    }
}