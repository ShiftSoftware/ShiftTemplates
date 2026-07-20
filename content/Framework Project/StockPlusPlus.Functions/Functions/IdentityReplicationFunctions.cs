#if (internalShiftIdentityHosting)
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ShiftSoftware.ShiftEntity.CosmosDbReplication.Services;
using ShiftSoftware.ShiftEntity.Model.Replication;
using ShiftSoftware.ShiftIdentity.AzureFunctions.Replication;
using StockPlusPlus.Data.DbContext;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace StockPlusPlus.Functions.Functions;

// Scheduled Cosmos catch-up replication for the hosted ShiftIdentity domain. There is ONE hourly timer per entity
// (each an incremental, dirty-only re-sync) plus ONE on-demand HTTP endpoint that re-syncs EVERYTHING (a full
// backfill). All mapping is manual — the ToXModel() delegates in ShiftIdentity.Data — via the reusable
// IdentityCatchUpReplicationExtensions service; AutoMapper is not involved.
//
// The timers are DISABLED in local dev two ways: (1) the host-honored AzureWebJobs.<FunctionName>.Disabled settings
// in local.settings.json stop them from firing at all locally (those settings are absent on a deployed host, so
// replication runs there); (2) RunTimerAsync also no-ops when IHostEnvironment.IsDevelopment() — a code-level
// backstop for any Development host. Either way, call POST api/replicate-all to sync on demand while developing.
public class IdentityReplicationFunctions
{
    // Top of every hour. Change here to re-schedule every per-entity timer at once.
    private const string HourlySchedule = "0 0 * * * *";

    private readonly CosmosDBReplication cosmos;
    private readonly IConfiguration configuration;
    private readonly IHostEnvironment environment;
    private readonly ILogger<IdentityReplicationFunctions> logger;

    public IdentityReplicationFunctions(
        CosmosDBReplication cosmos,
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger<IdentityReplicationFunctions> logger)
    {
        this.cosmos = cosmos;
        this.configuration = configuration;
        this.environment = environment;
        this.logger = logger;
    }

    // ─────────────────────────── per-entity hourly timers (disabled in Development) ───────────────────────────

    [Function(nameof(ReplicateServicesTimer))]
    public Task ReplicateServicesTimer([TimerTrigger(HourlySchedule)] TimerInfo timer)
        => RunTimerAsync((c, conn, db) => c.ReplicateServiceAsync<DB>(conn, db));

    [Function(nameof(ReplicateCompanyBranchServicesTimer))]
    public Task ReplicateCompanyBranchServicesTimer([TimerTrigger(HourlySchedule)] TimerInfo timer)
        => RunTimerAsync((c, conn, db) => c.ReplicateCompanyBranchServiceAsync<DB>(conn, db));

    [Function(nameof(ReplicateCompanyBranchDepartmentsTimer))]
    public Task ReplicateCompanyBranchDepartmentsTimer([TimerTrigger(HourlySchedule)] TimerInfo timer)
        => RunTimerAsync((c, conn, db) => c.ReplicateCompanyBranchDepartmentAsync<DB>(conn, db));

    [Function(nameof(ReplicateDepartmentsTimer))]
    public Task ReplicateDepartmentsTimer([TimerTrigger(HourlySchedule)] TimerInfo timer)
        => RunTimerAsync((c, conn, db) => c.ReplicateDepartmentAsync<DB>(conn, db));

    [Function(nameof(ReplicateBrandsTimer))]
    public Task ReplicateBrandsTimer([TimerTrigger(HourlySchedule)] TimerInfo timer)
        => RunTimerAsync((c, conn, db) => c.ReplicateBrandAsync<DB>(conn, db));

    [Function(nameof(ReplicateCompanyBranchBrandsTimer))]
    public Task ReplicateCompanyBranchBrandsTimer([TimerTrigger(HourlySchedule)] TimerInfo timer)
        => RunTimerAsync((c, conn, db) => c.ReplicateCompanyBranchBrandAsync<DB>(conn, db));

    [Function(nameof(ReplicateRegionsTimer))]
    public Task ReplicateRegionsTimer([TimerTrigger(HourlySchedule)] TimerInfo timer)
        => RunTimerAsync((c, conn, db) => c.ReplicateRegionAsync<DB>(conn, db));

    [Function(nameof(ReplicateCountriesTimer))]
    public Task ReplicateCountriesTimer([TimerTrigger(HourlySchedule)] TimerInfo timer)
        => RunTimerAsync((c, conn, db) => c.ReplicateCountryAsync<DB>(conn, db));

    [Function(nameof(ReplicateCitiesTimer))]
    public Task ReplicateCitiesTimer([TimerTrigger(HourlySchedule)] TimerInfo timer)
        => RunTimerAsync((c, conn, db) => c.ReplicateCityAsync<DB>(conn, db));

    [Function(nameof(ReplicateCompanyBranchesTimer))]
    public Task ReplicateCompanyBranchesTimer([TimerTrigger(HourlySchedule)] TimerInfo timer)
        => RunTimerAsync((c, conn, db) => c.ReplicateCompanyBranchAsync<DB>(conn, db));

    [Function(nameof(ReplicateCompaniesTimer))]
    public Task ReplicateCompaniesTimer([TimerTrigger(HourlySchedule)] TimerInfo timer)
        => RunTimerAsync((c, conn, db) => c.ReplicateCompanyAsync<DB>(conn, db));

    [Function(nameof(ReplicateTeamsTimer))]
    public Task ReplicateTeamsTimer([TimerTrigger(HourlySchedule)] TimerInfo timer)
        => RunTimerAsync((c, conn, db) => c.ReplicateTeamAsync<DB>(conn, db));

    [Function(nameof(ReplicateUsersTimer))]
    public Task ReplicateUsersTimer([TimerTrigger(HourlySchedule)] TimerInfo timer)
        => RunTimerAsync((c, conn, db) => c.ReplicateUserAsync<DB>(conn, db));

    // ─────────────────────────── on-demand: replicate EVERYTHING (full re-sync) ───────────────────────────

    [Function(nameof(ReplicateAllHttp))]
    public async Task<IActionResult> ReplicateAllHttp(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "replicate-all")] HttpRequest req)
    {
        if (!TryGetCosmos(out var connectionString, out var databaseId))
            return new OkObjectResult(new { replicated = false, reason = "CosmosDb replication is disabled or unconfigured." });

        logger.LogInformation("Identity full replication started at {start}.", DateTime.UtcNow);
        await cosmos.ReplicateAllAsync<DB>(connectionString, databaseId, updateAll: true);
        logger.LogInformation("Identity full replication finished at {end}.", DateTime.UtcNow);

        return new OkObjectResult(new { replicated = true });
    }

    // ─────────────────────────── helpers ───────────────────────────

    // Runs one entity's catch-up. Backstop to the host-level disable (local.settings.json): also skips in a
    // Development host, then no-ops when Cosmos isn't configured. `caller` is the compiler-supplied function name.
    private async Task RunTimerAsync(Func<CosmosDBReplication, string, string, Task> replicate, [CallerMemberName] string caller = "")
    {
        if (environment.IsDevelopment())
        {
            logger.LogInformation("{caller} skipped: replication timers are disabled in Development.", caller);
            return;
        }

        if (!TryGetCosmos(out var connectionString, out var databaseId))
        {
            logger.LogInformation("{caller} skipped: CosmosDb replication is disabled or unconfigured.", caller);
            return;
        }

        logger.LogInformation("{caller} started at {start}.", caller, DateTime.UtcNow);
        await replicate(cosmos, connectionString, databaseId);
        logger.LogInformation("{caller} finished at {end}.", caller, DateTime.UtcNow);
    }

    private bool TryGetCosmos(out string connectionString, out string databaseId)
    {
        databaseId = IdentityDatabaseAndContainerNames.DatabaseName;
        connectionString = configuration["CosmosDb:ConnectionString"] ?? string.Empty;
        var enabled = configuration.GetValue<bool>("CosmosDb:Enabled");
        return enabled && !string.IsNullOrWhiteSpace(connectionString);
    }
}
#endif
