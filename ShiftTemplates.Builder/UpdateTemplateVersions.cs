using System.Reflection;
using System.Xml;

namespace ShiftTemplates.Builder;

public class UpdateTemplateVersions
{
    public void Update()
    {
        Console.WriteLine("---------------------------------------------------------------------");
        Console.WriteLine("Updating Versions in template.json with whats available in ShiftFrameworkGlobalSettings.props");

        var projectPath = Tools.GetProjectPath();

        Console.WriteLine();
        Console.Write($"Project Path is: {projectPath}");

        Console.WriteLine();
        Console.WriteLine();

        var xmlPath = $"{projectPath}/ShiftFrameworkGlobalSettings.props";
        var xmlContent = File.ReadAllText(xmlPath);

        // Parse XML content
        var xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(xmlContent);

        // Create namespace manager
        var nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
        nsmgr.AddNamespace("ns", "http://schemas.microsoft.com/developer/msbuild/2003");

        // Extract values
        var shiftFrameworkVersion = xmlDoc.SelectSingleNode("//ns:Project/ns:PropertyGroup/ns:ShiftFrameworkVersion", nsmgr)!.InnerText;
        var typeAuthVersion = xmlDoc.SelectSingleNode("//ns:Project/ns:PropertyGroup/ns:TypeAuthVersion", nsmgr)!.InnerText;
        var azureFunctionsAspNetCoreAuthorizationVersion = xmlDoc.SelectSingleNode("//ns:Project/ns:PropertyGroup/ns:AzureFunctionsAspNetCoreAuthorizationVersion", nsmgr)!.InnerText;

        
        Console.WriteLine();
        Console.WriteLine($"Framework:\t\t\t\t {shiftFrameworkVersion}");
        Console.WriteLine($"TypeAuth:\t\t\t\t {typeAuthVersion}");
        Console.WriteLine($"Az Fun AspNetCore Authorization:\t {azureFunctionsAspNetCoreAuthorizationVersion}");
        Console.WriteLine();
        

        // Define template JSON path
        var templateJsonPath = @$"{projectPath}\content\Framework Project\.template.config\template.json";

        // Read and parse template JSON content
        var templateJsonContent = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(templateJsonPath));

        // Update JSON values
        templateJsonContent!["symbols"]!["frameworkVersion"]!["parameters"]!["value"] = shiftFrameworkVersion;
        templateJsonContent!["symbols"]!["typeAuthVersion"]!["parameters"]!["value"] = typeAuthVersion;
        templateJsonContent!["symbols"]!["azureFunctionsAspNetCoreAuthorizationVersion"]!["parameters"]!["value"] = azureFunctionsAspNetCoreAuthorizationVersion;

        //// Write back updated JSON content
        File.WriteAllText(templateJsonPath, templateJsonContent.ToString());

        Console.WriteLine("Versions in template.json are updated");
        Console.WriteLine("---------------------------------------------------------------------");
    }
}