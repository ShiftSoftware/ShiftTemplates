using System.Diagnostics;
using System.Text.RegularExpressions;

namespace ShiftTemplates.Builder;

public class MultiConfigRunner
{
    private const string IntSrv = "int-srv.local";
    private const string IntWasm = "int-wasm.local";
    private const string ExtSrv = "ext-srv.local";
    private const string ExtWasm = "ext-wasm.local";
    private const string Web = "web.local";

    private const int WebPort = 5105;

    // Generated projects land under ShiftTemplates/BuilderOutput/ so the root stays clean.
    // This depth (csproj at ShiftTemplates\BuilderOutput\TestProject-X\<Project>\) is also
    // what the template's hardcoded ..\..\..\..\ framework refs and ..\..\..\ props import
    // were designed for, which is why no path rewriting / props copy is needed.
    private const string OutputDirName = "BuilderOutput";

    private record ServerConfig(
        string FolderSuffix,
        string IdentityType,
        string RenderMode,
        int Port,
        string Hostname,
        string? ExternalIdentityApi,
        string? ExternalIdentityFrontEnd);

    public void Run(bool launchProcesses)
    {
        var projectPath = Tools.GetProjectPath();
        var outputRoot = Path.Combine(projectPath, OutputDirName);
        Directory.CreateDirectory(outputRoot);

        var configs = new[]
        {
            new ServerConfig("Internal-Server",  "Internal", "Server",      5101, IntSrv,  null,                        null),
            new ServerConfig("Internal-WASM",    "Internal", "WebAssembly", 5102, IntWasm, null,                        null),
            new ServerConfig("External-Server",  "External", "Server",      5103, ExtSrv,  $"https://{IntSrv}:5101",    $"https://{IntSrv}:5101"),
            new ServerConfig("External-WASM",    "External", "WebAssembly", 5104, ExtWasm, $"https://{IntWasm}:5102",   $"https://{IntWasm}:5102"),
        };

        var creator = new CreateProject();
        foreach (var cfg in configs)
        {
            var folder = ResolveFolder(projectPath, cfg.FolderSuffix);
            var isWebHost = cfg.FolderSuffix == "Internal-Server";

            creator.Create(
                outputFolder: folder,
                includeSampleApp: true,
                identityType: cfg.IdentityType,
                renderMode: cfg.RenderMode,
                serverPort: cfg.Port,
                serverHostname: cfg.Hostname,
                addTest: false,
                addFunctions: false,
                externalIdentityApi: cfg.ExternalIdentityApi,
                externalIdentityFrontEnd: cfg.ExternalIdentityFrontEnd,
                webPort: isWebHost ? WebPort : null,
                webHostname: isWebHost ? Web : null,
                webBaseUrl: isWebHost ? $"https://{IntSrv}:5101/api/" : null,
                webIdentityApi: isWebHost ? $"https://{IntSrv}:5101/api/" : null,
                webIdentityFrontEnd: isWebHost ? $"https://{IntSrv}:5101" : null);
        }

        PrintHostsFileBlock();

        MigrateDatabase(projectPath);

        if (!launchProcesses)
        {
            Console.WriteLine("--skip-launch set; not starting dotnet run processes.");
            return;
        }

        Console.WriteLine();
        Console.WriteLine("=====================================================================");
        Console.WriteLine("Building all projects before launch...");
        Console.WriteLine("=====================================================================");
        Console.WriteLine();

        var projectsToBuild = new List<(string label, string folder)>();

        foreach (var cfg in configs)
        {
            var folder = ResolveFolder(projectPath, cfg.FolderSuffix);
            projectsToBuild.Add(($"Server  {cfg.FolderSuffix}", Path.Combine(folder, "Test.Server")));
        }

        projectsToBuild.Add(("Web     Standalone", Path.Combine(ResolveFolder(projectPath, "Internal-Server"), "Test.Web")));

        foreach (var (label, folder) in projectsToBuild)
        {
            Console.WriteLine($"  Building {label}...");
            RunDotnet("build", workingDirectory: folder);
        }

        Console.WriteLine();

        var processes = new List<(string label, string url, Process proc)>();

        void KillAll()
        {
            foreach (var p in processes)
            {
                try
                {
                    if (!p.proc.HasExited)
                        p.proc.Kill(entireProcessTree: true);
                }
                catch { }
                finally
                {
                    p.proc.Dispose();
                }
            }
        }

        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            KillAll();
            Environment.Exit(0);
        };

        AppDomain.CurrentDomain.ProcessExit += (_, _) => KillAll();

        try
        {
            foreach (var cfg in configs)
            {
                var folder = ResolveFolder(projectPath, cfg.FolderSuffix);
                var serverProject = Path.Combine(folder, "Test.Server");
                var proc = StartDotnetRun(serverProject);
                processes.Add(($"Server  {cfg.FolderSuffix}", $"https://{cfg.Hostname}:{cfg.Port}", proc));
            }

            // Standalone WASM Web runs from the Internal-Server folder, pointing at IntSrv:5101.
            // Web itself stays http (JWT/localStorage; no Secure-cookie constraint).
            {
                var folder = ResolveFolder(projectPath, "Internal-Server");
                var webProject = Path.Combine(folder, "Test.Web");
                var proc = StartDotnetRun(webProject);
                processes.Add(("Web     Standalone", $"http://{Web}:{WebPort}", proc));
            }

            Console.WriteLine();
            Console.WriteLine("=====================================================================");
            Console.WriteLine("All 5 processes started. Press ENTER or Ctrl+C to stop them.");
            Console.WriteLine("=====================================================================");
            Console.WriteLine();
            foreach (var p in processes)
                Console.WriteLine($"  {p.label,-32}  {p.url}");
            Console.WriteLine();

            Console.ReadLine();
        }
        finally
        {
            Console.WriteLine("Stopping processes...");
            KillAll();
            Console.WriteLine("Done.");
        }
    }

    private static string ResolveFolder(string projectPath, string suffix)
        => Path.GetFullPath(Path.Combine(projectPath, OutputDirName, $"TestProject-{suffix}"));

    // Scaffold + apply migrations against the dedicated ShiftTemplates.Builder.Migrator project,
    // which owns the schema for the shared TestNET10Auto DB. We name each migration with a UTC
    // timestamp so EF actually diffs the current model against the persisted snapshot — if we
    // reused a fixed name like "InitialCreate", EF would see it in __EFMigrationsHistory and
    // skip, leaving the DB schema drifted from the model after framework changes.
    //
    // `dotnet ef migrations add` always writes a file even when there are no model changes, so
    // we prune the empty result to keep the Migrations/ folder free of noise.
    private static void MigrateDatabase(string projectPath)
    {
        var migratorPath = Path.GetFullPath(Path.Combine(projectPath, "ShiftTemplates.Builder.Migrator"));
        var migrationName = $"Auto_{DateTime.UtcNow:yyyyMMddHHmmss}";

        Console.WriteLine();
        Console.WriteLine("=====================================================================");
        Console.WriteLine($"Migrate: ShiftTemplates.Builder.Migrator -> TestNET10Auto");
        Console.WriteLine("=====================================================================");
        Console.WriteLine();

        // dotnet-ef is pinned in .config/dotnet-tools.json. Restore is a no-op when already
        // installed and avoids a "tool not found" failure on a fresh clone.
        RunDotnet("tool restore", workingDirectory: projectPath);

        // --startup-project must point at the Migrator itself. If left unset, dotnet ef
        // uses the working directory's project — which here is ShiftTemplates.csproj, a
        // NuGet template package with no build output, and EF then fails looking for
        // Migrator.dll inside its empty bin folder.
        var efArgs = $"--project \"{migratorPath}\" --startup-project \"{migratorPath}\"";
        RunDotnet($"ef migrations add {migrationName} {efArgs}", workingDirectory: projectPath);
        PruneEmptyMigration(migratorPath, migrationName);
        RunDotnet($"ef database update {efArgs}", workingDirectory: projectPath);

        Console.WriteLine();
        Console.WriteLine("---------------------------------------------------------------------");
    }

    private static void RunDotnet(string arguments, string? workingDirectory = null)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = arguments,
            UseShellExecute = false,
            WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory,
        };
        using var proc = Process.Start(psi)
            ?? throw new InvalidOperationException($"Failed to start: dotnet {arguments}");
        proc.WaitForExit();
        if (proc.ExitCode != 0)
            throw new InvalidOperationException($"`dotnet {arguments}` exited with code {proc.ExitCode}.");
    }

    // dotnet ef writes Migrations\<timestamp>_<name>.cs + .Designer.cs and updates the snapshot
    // file. When the current model matches the snapshot, the new migration's Up/Down bodies are
    // empty — delete it so empty migrations don't accumulate in git. The snapshot is unchanged
    // when the model is unchanged, so it needs no rollback.
    private static void PruneEmptyMigration(string migratorPath, string migrationName)
    {
        var migrationsDir = Path.Combine(migratorPath, "Migrations");
        if (!Directory.Exists(migrationsDir))
            return;

        var migrationFile = Directory.EnumerateFiles(migrationsDir, $"*_{migrationName}.cs")
            .FirstOrDefault(f => !f.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase));
        if (migrationFile is null)
            return;

        var content = File.ReadAllText(migrationFile);
        if (!IsEmptyMigration(content))
            return;

        File.Delete(migrationFile);

        var designerFile = migrationFile[..^".cs".Length] + ".Designer.cs";
        if (File.Exists(designerFile))
            File.Delete(designerFile);

        Console.WriteLine($"  Pruned empty migration: {Path.GetFileNameWithoutExtension(migrationFile)}");
    }

    private static bool IsEmptyMigration(string content)
    {
        // Both Up and Down must have empty bodies (allowing whitespace/newlines only).
        var upEmpty = Regex.IsMatch(content,
            @"protected\s+override\s+void\s+Up\s*\(\s*MigrationBuilder\s+\w+\s*\)\s*\{\s*\}");
        var downEmpty = Regex.IsMatch(content,
            @"protected\s+override\s+void\s+Down\s*\(\s*MigrationBuilder\s+\w+\s*\)\s*\{\s*\}");
        return upEmpty && downEmpty;
    }

    private static void PrintHostsFileBlock()
    {
        Console.WriteLine();
        Console.WriteLine("=====================================================================");
        Console.WriteLine("One-time setup");
        Console.WriteLine("=====================================================================");
        Console.WriteLine();
        Console.WriteLine(@"1. Hosts file (requires admin) — add this line to C:\Windows\System32\drivers\etc\hosts:");
        Console.WriteLine();
        Console.WriteLine($"     127.0.0.1  {IntSrv} {IntWasm} {ExtSrv} {ExtWasm} {Web}");
        Console.WriteLine();
        Console.WriteLine("2. Dev cert — run once per machine to generate + trust a multi-SAN cert:");
        Console.WriteLine();
        Console.WriteLine(@"     powershell -ExecutionPolicy Bypass -File ShiftTemplates\devcerts\Setup-DevCert.ps1");
        Console.WriteLine();
        Console.WriteLine("URLs once setup is complete:");
        Console.WriteLine();
        Console.WriteLine($"  https://{IntSrv}:5101    Blazor Web App, Internal identity, Server render");
        Console.WriteLine($"  https://{IntWasm}:5102   Blazor Web App, Internal identity, WebAssembly render");
        Console.WriteLine($"  https://{ExtSrv}:5103    Blazor Web App, External identity, Server render");
        Console.WriteLine($"  https://{ExtWasm}:5104   Blazor Web App, External identity, WebAssembly render");
        Console.WriteLine($"  http://{Web}:5105        Standalone WASM (JWT, points at {IntSrv}:5101)");
        Console.WriteLine();
        Console.WriteLine("All 4 Server apps share the SQL database 'StockPlusPlusNET10Auto'.");
        Console.WriteLine("Running >1 Internal app concurrently may race on EF migrations.");
        Console.WriteLine();
    }

    private static Process StartDotnetRun(string projectFolder)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "run --no-build",
            WorkingDirectory = projectFolder,
            UseShellExecute = false,
            CreateNoWindow = false,
        };
        return Process.Start(psi)!;
    }
}
