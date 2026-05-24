using System.Net;
using Microsoft.Azure.Cosmos;
using ShiftSoftware.ShiftEntity.Model.Replication;

namespace StockPlusPlus.Test.Tests;

public class IdentityCosmosSetupTests
{
    private readonly ITestOutputHelper output;

    public IdentityCosmosSetupTests(ITestOutputHelper output)
    {
        this.output = output;
    }

    [Fact]
    public async Task Create_Identity_Database_And_Containers()
    {
        var connectionString = GetCosmosConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            output.WriteLine("No Cosmos connection string configured. Skipping.");
            return;
        }

        using var client = new CosmosClient(connectionString);

        //Create the database with SHARED throughput so containers don't each consume a dedicated emulator partition.
        //Without this the local Cosmos emulator runs out of its default partition budget and starts returning 503/1007.
        var databaseResponse = await RetryOnTransientAsync(
            ct => client.CreateDatabaseIfNotExistsAsync(IdentityDatabaseAndContainerNames.DatabaseName, throughput: 10000, cancellationToken: ct),
            TestContext.Current.CancellationToken);
        var database = databaseResponse.Database;
        output.WriteLine($"Database '{database.Id}' ready (status: {databaseResponse.StatusCode}).");

        var containers = new (string Name, IReadOnlyList<string> PartitionKeyPaths)[]
        {
            (IdentityDatabaseAndContainerNames.CountryContainerName, new[] { "/CountryID", "/RegionID", "/ItemType" }),
            (IdentityDatabaseAndContainerNames.CompanyContainerName, new[] { "/id" }),
            (IdentityDatabaseAndContainerNames.CompanyBranchContainerName, new[] { "/BranchID", "/ItemType" }),
            (IdentityDatabaseAndContainerNames.ServiceContainerName, new[] { "/id" }),
            (IdentityDatabaseAndContainerNames.DepartmentContainerName, new[] { "/id" }),
            (IdentityDatabaseAndContainerNames.TeamContainerName, new[] { "/id" }),
            (IdentityDatabaseAndContainerNames.BrandContainerName, new[] { "/id" }),
            (IdentityDatabaseAndContainerNames.UserContainerName, new[] { "/id" }),
        };

        foreach (var (name, partitionKeyPaths) in containers)
        {
            var properties = new ContainerProperties(name, partitionKeyPaths.ToList());

            var response = await RetryOnTransientAsync(
                ct => database.CreateContainerIfNotExistsAsync(properties, cancellationToken: ct),
                TestContext.Current.CancellationToken);

            output.WriteLine($"Container '{name}' ready (status: {response.StatusCode}, partitionKeys: {string.Join(", ", partitionKeyPaths)}).");
        }
    }

    //The local Cosmos emulator throttles rapid container/database creates with 503 (substatus 1007 — "experiencing high demand").
    //Real Azure can also surface transient 503/429s. A short exponential backoff keeps the test reliable in both environments.
    private async Task<T> RetryOnTransientAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken)
    {
        const int maxAttempts = 5;
        var delay = TimeSpan.FromSeconds(1);

        for (int attempt = 1; ; attempt++)
        {
            try
            {
                return await action(cancellationToken);
            }
            catch (CosmosException ex) when (attempt < maxAttempts && IsTransient(ex))
            {
                output.WriteLine($"Transient Cosmos error ({(int)ex.StatusCode}/{ex.SubStatusCode}) on attempt {attempt}; retrying in {delay.TotalSeconds:F0}s.");
                await Task.Delay(delay, cancellationToken);
                delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, 16));
            }
        }
    }

    private static bool IsTransient(CosmosException ex) =>
        ex.StatusCode == HttpStatusCode.ServiceUnavailable ||
        ex.StatusCode == HttpStatusCode.TooManyRequests ||
        ex.StatusCode == HttpStatusCode.RequestTimeout;

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
}
