using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ShiftSoftware.ShiftEntity.CosmosDbReplication.Services;
using ShiftSoftware.ShiftEntity.Model.Replication;
using ShiftSoftware.ShiftEntity.Model.Replication.IdentityModels;
using ShiftSoftware.ShiftIdentity.Core.Entities;
using StockPlusPlus.Data.DbContext;

namespace StockPlusPlus.Functions.Functions;

public class CitySync
{
    private readonly CosmosDBReplication cosmosDBReplication;
    private readonly IConfiguration configuration;
    private readonly ILogger<CitySync> logger;

    public CitySync(CosmosDBReplication cosmosDBReplication, IConfiguration configuration, ILogger<CitySync> logger)
    {
        this.cosmosDBReplication = cosmosDBReplication;
        this.configuration = configuration;
        this.logger = logger;
    }

    //[Function(nameof(SyncCitiesTimer))]
    //public Task SyncCitiesTimer([TimerTrigger("0 */5 * * * *")] TimerInfo myTimer)
    //    => SyncCitiesAsync();

    [Function(nameof(SyncCitiesHttp))]
    public async Task<IActionResult> SyncCitiesHttp(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "sync/cities")] HttpRequest req)
    {
        await SyncCitiesAsync();
        return new OkObjectResult(new { synced = true });
    }

    private async Task SyncCitiesAsync()
    {
        var cosmosEnabled = configuration.GetValue<bool>("CosmosDb:Enabled");
        var connectionString = configuration["CosmosDb:ConnectionString"];

        if (!cosmosEnabled || string.IsNullOrWhiteSpace(connectionString))
        {
            logger.LogInformation("CosmosDb replication is disabled or unconfigured; skipping city sync.");
            return;
        }

        logger.LogInformation("City sync started at {start}.", DateTime.UtcNow);

        await cosmosDBReplication
            .SetUp<DB, City>(connectionString, IdentityDatabaseAndContainerNames.DatabaseName)
            .Replicate<CityModel>(IdentityDatabaseAndContainerNames.CountryContainerName)
            .RunAsync();

        logger.LogInformation("City sync finished at {end}.", DateTime.UtcNow);
    }
}
