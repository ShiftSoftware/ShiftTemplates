using System.Diagnostics;
using System.Text.RegularExpressions;

namespace ShiftTemplates.Builder;

public class CreateProject
{
    public CreateProject Create(
        string outputFolder,
        bool includeSampleApp,
        string identityType,
        string renderMode,
        int serverPort,
        string serverHostname,
        bool addTest = false,
        bool addFunctions = false,
        string? externalIdentityApi = null,
        string? externalIdentityFrontEnd = null,
        int? webPort = null,
        string? webHostname = null,
        string? webBaseUrl = null,
        string? webIdentityApi = null,
        string? webIdentityFrontEnd = null)
    {
        var label = Path.GetFileName(outputFolder);

        Console.WriteLine();
        Console.WriteLine("---------------------------------------------------------------------");
        Console.WriteLine($"Generate: {label} (identity={identityType}, render={renderMode}, port={serverPort})");
        Console.WriteLine();

        if (Directory.Exists(outputFolder))
            Directory.Delete(outputFolder, true);

        var args =
            $"new shift " +
            $"--includeSampleApp {includeSampleApp} " +
            $"--shiftIdentityHostingType {identityType} " +
            $"--renderMode {renderMode} " +
            $"--addTest {addTest} " +
            $"--addFunctions {addFunctions} " +
            $"-n Test " +
            $"--output \"{outputFolder}\"";

        if (!string.IsNullOrWhiteSpace(externalIdentityApi))
            args += $" --externalShiftIdentityApi \"{externalIdentityApi}\"";

        if (!string.IsNullOrWhiteSpace(externalIdentityFrontEnd))
            args += $" --externalShiftIdentityFrontEnd \"{externalIdentityFrontEnd}\"";

        var process = Process.Start("dotnet", args);
        process!.WaitForExit(-1);

        PostProcessServer(outputFolder, serverPort, serverHostname, identityType);
        PostProcessClient(outputFolder, serverPort, serverHostname, identityType);

        if (webPort.HasValue && webHostname is not null)
        {
            PostProcessWeb(
                outputFolder,
                webPort.Value,
                webHostname,
                webBaseUrl ?? $"https://{serverHostname}:{serverPort}/api/",
                webIdentityApi ?? $"https://{serverHostname}:{serverPort}/api/",
                webIdentityFrontEnd ?? $"https://{serverHostname}:{serverPort}");
        }

        Console.WriteLine();
        Console.WriteLine("---------------------------------------------------------------------");

        return this;
    }

    // Matches the `-n Test` passed to `dotnet new shift`; the template renames its sourceName
    // `StockPlusPlus` to `Test` everywhere, so generated project folders are Test.Server, Test.Web, etc.
    private const string ProjectName = "Test";

    private static void PostProcessServer(string outputFolder, int port, string hostname, string identityType)
    {
        var serverLaunch = Path.Combine(outputFolder, $"{ProjectName}.Server", "Properties", "launchSettings.json");
        WriteServerLaunchSettings(serverLaunch, port);

        var serverAppsettings = Path.Combine(outputFolder, $"{ProjectName}.Server", "appsettings.Development.json");
        RewriteJsonValue(serverAppsettings, "BaseURL", $"https://{hostname}:{port}/api/");

        if (identityType.Equals("Internal", StringComparison.OrdinalIgnoreCase))
        {
            // For Internal hosting the template's externalShiftIdentityApi parameter is empty,
            // which causes its "replaces: http://localhost:5069" rule to strip the host prefix
            // out of these two keys. Restore them to point at this server's own URL.
            RewriteJsonValue(serverAppsettings, "ShiftIdentityApi", $"https://{hostname}:{port}/api/");
            RewriteJsonValue(serverAppsettings, "ShiftIdentityFrontEnd", $"https://{hostname}:{port}");
        }
    }

    // Test.Client is the Blazor Web App's WASM component library. Its wwwroot
    // appsettings.Development.json is served by the host and read by the WASM at startup,
    // so its BaseURL must point at the host (same origin) for the cookie to flow back on
    // refresh polls. The template ships with localhost:5079 baked in and `dotnet new shift`
    // does not rewrite it, so we sync it to the host here. ShiftIdentityApi/FrontEnd are
    // already correctly set by the template's `replaces` rule for External hosting; for
    // Internal we mirror the Server's appsettings so the two halves agree.
    private static void PostProcessClient(string outputFolder, int port, string hostname, string identityType)
    {
        var clientAppsettings = Path.Combine(outputFolder, $"{ProjectName}.Client", "wwwroot", "appsettings.Development.json");
        if (!File.Exists(clientAppsettings))
            return;

        RewriteJsonValue(clientAppsettings, "BaseURL", $"https://{hostname}:{port}/api/");

        if (identityType.Equals("Internal", StringComparison.OrdinalIgnoreCase))
        {
            RewriteJsonValue(clientAppsettings, "ShiftIdentityApi", $"https://{hostname}:{port}/api/");
            RewriteJsonValue(clientAppsettings, "ShiftIdentityFrontEnd", $"https://{hostname}:{port}");
        }
    }

    private static void PostProcessWeb(
        string outputFolder,
        int webPort,
        string webHostname,
        string baseUrl,
        string identityApi,
        string identityFrontEnd)
    {
        // Test.Web is the standalone WASM (JWT, localStorage). It doesn't need HTTPS for
        // cookie reasons, so we leave it on http for now. Its outbound calls to the cookie
        // host go to https URLs (set in the wwwroot appsettings below).
        var webLaunch = Path.Combine(outputFolder, $"{ProjectName}.Web", "Properties", "launchSettings.json");
        RewriteJsonValue(webLaunch, "applicationUrl", $"http://*:{webPort}");

        var webAppsettings = Path.Combine(outputFolder, $"{ProjectName}.Web", "wwwroot", "appsettings.Development.json");
        RewriteJsonValue(webAppsettings, "BaseURL", baseUrl);
        RewriteJsonValue(webAppsettings, "ShiftIdentityApi", identityApi);
        RewriteJsonValue(webAppsettings, "ShiftIdentityFrontEnd", identityFrontEnd);
    }

    // Writes launchSettings.json with an https profile that points Kestrel at the shared
    // dev PFX generated by ShiftTemplates/devcerts/Setup-DevCert.ps1. The path is relative
    // to the project working directory (`dotnet run` runs with cwd=project folder), so
    // ../../../devcerts/dev.pfx from ShiftTemplates/BuilderOutput/TestProject-X/Test.Server/
    // resolves to ShiftTemplates/devcerts/dev.pfx.
    private static void WriteServerLaunchSettings(string filePath, int port)
    {
        var content = $@"{{
  ""$schema"": ""https://json.schemastore.org/launchsettings.json"",
  ""profiles"": {{
    ""https"": {{
      ""commandName"": ""Project"",
      ""dotnetRunMessages"": true,
      ""launchBrowser"": true,
      ""applicationUrl"": ""https://*:{port}"",
      ""environmentVariables"": {{
        ""ASPNETCORE_ENVIRONMENT"": ""Development"",
        ""ASPNETCORE_Kestrel__Certificates__Default__Path"": ""../../../devcerts/dev.pfx"",
        ""ASPNETCORE_Kestrel__Certificates__Default__Password"": ""devcert""
      }}
    }}
  }}
}}
";
        var dir = Path.GetDirectoryName(filePath);
        if (dir is not null)
            Directory.CreateDirectory(dir);
        File.WriteAllText(filePath, content);
    }

    private static void RewriteJsonValue(string filePath, string key, string newValue)
    {
        if (!File.Exists(filePath))
            return;

        var content = File.ReadAllText(filePath);

        // Anchor to start-of-line + whitespace so we skip commented-out lines (`//"key": "..."`)
        // and don't accidentally rewrite reference URLs the template leaves as comments.
        var pattern = $@"(?m)^(?<prefix>\s*""{Regex.Escape(key)}""\s*:\s*)""[^""]*""";
        var replaced = Regex.Replace(
            content,
            pattern,
            m => $"{m.Groups["prefix"].Value}\"{newValue}\"");

        if (content != replaced)
            File.WriteAllText(filePath, replaced);
    }
}
