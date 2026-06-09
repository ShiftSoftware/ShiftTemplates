using System.Diagnostics;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using ShiftSoftware.ShiftIdentity.Data.Cosmos.Options;
using ShiftSoftware.ShiftIdentity.Data.Cosmos.Services;

namespace StockPlusPlus.Test.Tests;

public class IdentityReferenceCosmosDataServiceTests
{
    private const int CallsPerFunction = 5;
    private readonly ITestOutputHelper output;

    public IdentityReferenceCosmosDataServiceTests(ITestOutputHelper output)
    {
        this.output = output;
    }

    [Fact]
    public async Task Benchmark_All_Service_Functions_Five_Times()
    {
        var setup = CreateSetup();
        if (setup is null)
            return;

        using var scope = setup;
        var rows = new List<TimingRow>();

        var countries = await scope.Service.GetCountriesAsync();
        var regions = await scope.Service.GetRegionsAsync();
        var cities = await scope.Service.GetCitiesAsync();
        var companies = await scope.Service.GetCompaniesAsync();
        var branches = await scope.Service.GetCompanyBranchesAsync();
        var services = await scope.Service.GetServicesAsync();
        var departments = await scope.Service.GetDepartmentsAsync();
        var teams = await scope.Service.GetTeamsAsync();
        var brands = await scope.Service.GetBrandsAsync();

        await MeasureAsync("GetCountriesAsync", rows, async () =>
        {
            var result = await scope.Service.GetCountriesAsync();
            return $"count={result.Count}";
        });

        await MeasureAsync("GetRegionsAsync", rows, async () =>
        {
            var result = await scope.Service.GetRegionsAsync();
            return $"count={result.Count}";
        });

        await MeasureAsync("GetCitiesAsync", rows, async () =>
        {
            var result = await scope.Service.GetCitiesAsync();
            return $"count={result.Count}";
        });

        await MeasureAsync("GetCompaniesAsync", rows, async () =>
        {
            var result = await scope.Service.GetCompaniesAsync();
            return $"count={result.Count}";
        });

        await MeasureAsync("GetCompanyBranchesAsync", rows, async () =>
        {
            var result = await scope.Service.GetCompanyBranchesAsync();
            return $"count={result.Count}";
        });

        await MeasureAsync("GetServicesAsync", rows, async () =>
        {
            var result = await scope.Service.GetServicesAsync();
            return $"count={result.Count}";
        });

        await MeasureAsync("GetDepartmentsAsync", rows, async () =>
        {
            var result = await scope.Service.GetDepartmentsAsync();
            return $"count={result.Count}";
        });

        await MeasureAsync("GetTeamsAsync", rows, async () =>
        {
            var result = await scope.Service.GetTeamsAsync();
            return $"count={result.Count}";
        });

        await MeasureAsync("GetBrandsAsync", rows, async () =>
        {
            var result = await scope.Service.GetBrandsAsync();
            return $"count={result.Count}";
        });

        await MeasureByIdAsync("GetCountryByIdAsync", rows, scope.Service.GetCountryByIdAsync, countries.Keys.FirstOrDefault());
        await MeasureByIdAsync("GetRegionByIdAsync", rows, scope.Service.GetRegionByIdAsync, regions.Keys.FirstOrDefault());
        await MeasureByIdAsync("GetCityByIdAsync", rows, scope.Service.GetCityByIdAsync, cities.Keys.FirstOrDefault());
        await MeasureByIdAsync("GetCompanyByIdAsync", rows, scope.Service.GetCompanyByIdAsync, companies.Keys.FirstOrDefault());
        await MeasureByIdAsync("GetCompanyBranchByIdAsync", rows, scope.Service.GetCompanyBranchByIdAsync, branches.Keys.FirstOrDefault());
        await MeasureByIdAsync("GetServiceByIdAsync", rows, scope.Service.GetServiceByIdAsync, services.Keys.FirstOrDefault());
        await MeasureByIdAsync("GetDepartmentByIdAsync", rows, scope.Service.GetDepartmentByIdAsync, departments.Keys.FirstOrDefault());
        await MeasureByIdAsync("GetTeamByIdAsync", rows, scope.Service.GetTeamByIdAsync, teams.Keys.FirstOrDefault());
        await MeasureByIdAsync("GetBrandByIdAsync", rows, scope.Service.GetBrandByIdAsync, brands.Keys.FirstOrDefault());

        WriteTable(rows);
    }

    private async Task MeasureByIdAsync<TModel>(
        string functionName,
        List<TimingRow> rows,
        Func<string, CancellationToken, Task<TModel?>> action,
        string? id)
        where TModel : class
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            rows.Add(new TimingRow(functionName, 0, 0, "skipped", "No id found in existing data"));
            return;
        }

        await MeasureAsync(functionName, rows, async () =>
        {
            var result = await action(id, CancellationToken.None);
            return result is null ? "miss" : "hit";
        });
    }

    private static async Task MeasureAsync(string functionName, List<TimingRow> rows, Func<Task<string>> action)
    {
        for (int call = 1; call <= CallsPerFunction; call++)
        {
            var watch = Stopwatch.StartNew();
            var result = await action();
            watch.Stop();

            rows.Add(new TimingRow(functionName, call, watch.Elapsed.TotalMilliseconds, result, null));
        }
    }

    private void WriteTable(List<TimingRow> rows)
    {
        output.WriteLine("Benchmark Results");
        output.WriteLine(new string('=', 80));

        foreach (var functionGroup in rows.GroupBy(x => x.FunctionName).OrderBy(x => x.Key))
        {
            output.WriteLine(string.Empty);
            output.WriteLine($"Function: {functionGroup.Key}");
            output.WriteLine("| Call | Time (ms) | Result | Note |");
            output.WriteLine("|---:|---:|---|---|");

            foreach (var row in functionGroup.OrderBy(x => x.CallNumber))
            {
                output.WriteLine($"| {row.CallNumber} | {row.ElapsedMilliseconds:F3} | {row.Result} | {row.Note ?? ""} |");
            }

            var measuredRows = functionGroup.Where(x => x.CallNumber > 0).ToList();
            if (measuredRows.Count > 0)
            {
                output.WriteLine($"Summary: avg={measuredRows.Average(x => x.ElapsedMilliseconds):F3} ms, min={measuredRows.Min(x => x.ElapsedMilliseconds):F3} ms, max={measuredRows.Max(x => x.ElapsedMilliseconds):F3} ms");
            }
        }
    }

    private static TestScope? CreateSetup()
    {
        var connectionString = GetCosmosConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
            return null;

        try
        {
            var client = new CosmosClient(connectionString);

            var options = Options.Create(new IdentityReferenceCosmosDataOptions());
            var service = new IdentityReferenceCosmosDataService<CosmosClient>(client, options);

            return new TestScope(client, service);
        }
        catch
        {
            return null;
        }
    }

    private static string? GetCosmosConnectionString()
    {
        var fromEnvironment = Environment.GetEnvironmentVariable("ConnectionStrings__CosmosDb")
            ?? Environment.GetEnvironmentVariable("CosmosDb__ConnectionString");
        if (!string.IsNullOrWhiteSpace(fromEnvironment))
            return fromEnvironment;

        var appSettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (!File.Exists(appSettingsPath))
            return null;

        using var stream = File.OpenRead(appSettingsPath);
        var root = JsonNode.Parse(stream);

        return root?["ConnectionStrings"]?["CosmosDb"]?.GetValue<string>()
            ?? root?["CosmosDb"]?["ConnectionString"]?.GetValue<string>();
    }

    private readonly record struct TimingRow(
        string FunctionName,
        int CallNumber,
        double ElapsedMilliseconds,
        string Result,
        string? Note);

    private sealed class TestScope : IDisposable
    {
        public TestScope(
        CosmosClient Client,
        IdentityReferenceCosmosDataService<CosmosClient> Service)
        {
            this.Client = Client;
            this.Service = Service;
        }

        public CosmosClient Client { get; }
        public IdentityReferenceCosmosDataService<CosmosClient> Service { get; }

        public void Dispose() => Client.Dispose();
    }
}
