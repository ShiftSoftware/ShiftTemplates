using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ShiftEntity.CosmosDbReplication.Services;
using ShiftSoftware.ShiftEntity.Model.Replication;
using ShiftSoftware.ShiftEntity.Model.Replication.IdentityModels;
using StockPlusPlus.Data.DbContext;

namespace StockPlusPlus.API.Controllers;

[Route("api/[controller]")]
[AllowAnonymous]
public class UtilityController : ControllerBase
{
    private readonly CosmosClient cosmosClient;
    private readonly IHostEnvironment hostEnvironment;
    private readonly CosmosDBReplication replication;

    public UtilityController(CosmosClient cosmosClient, IHostEnvironment hostEnvironment, CosmosDBReplication replication)
    {
        this.cosmosClient = cosmosClient;
        this.hostEnvironment = hostEnvironment;
        this.replication = replication;
    }

    [HttpGet("delete-all-data")]
    public async Task<ActionResult> DeleteAllData()
    {
        if (!this.hostEnvironment.IsDevelopment())
        {
            return BadRequest("This action is only allowed in development mode.");
        }

        await DeleteAllDataAsync(this.cosmosClient);

        return Ok();
    }

    [HttpGet("replicate-all")]
    public async Task<ActionResult> ReplicateAll()
    {
        if (!this.hostEnvironment.IsDevelopment())
        {
            return BadRequest("This action is only allowed in development mode.");
        }

        await ReplicateAll(this.replication, this.cosmosClient, "Identity");

        return Ok();
    }

    private static async Task RemoveItemsFromContainer<T>(
        Database database,
        string containerName,
        Func<T, string> idExtractor,
        Func<T, Microsoft.Azure.Cosmos.PartitionKey> partitionKey
    )
    {
        var container = database.GetContainer(containerName);

        var items = new List<T>();

        var iterator = container.GetItemQueryIterator<T>();

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();

            items.AddRange(response.ToList());
        }

        if (items.Count == 0)
        {
            return;
        }

        var tasks = new List<Task>();

        foreach (var item in items)
        {
            tasks.Add(
                container.DeleteItemAsync<T>(
                    idExtractor(item),
                    partitionKey.Invoke(item)
                )
            );
        }

        await Task.WhenAll(tasks);
    }

    public static async Task DeleteAllDataAsync(CosmosClient cosmosClient)
    {
        var identityDatabase = cosmosClient.GetDatabase("Identity");

        var tasks = new List<Task>
        {
            Task.Run(async () => {
                await RemoveItemsFromContainer<BrandModel>(
                    identityDatabase,
                    IdentityDatabaseAndContainerNames.BrandContainerName,
                    x => x.id,
                    x =>  new PartitionKeyBuilder().Add(x.id).Build()
                );
            }),

            Task.Run(async () => {
                await RemoveItemsFromContainer<CompanyModel>(
                    identityDatabase,
                    IdentityDatabaseAndContainerNames.CompanyContainerName,
                    x => x.id,
                    x =>  new PartitionKeyBuilder().Add(x.id).Build()
                );
            }),

            Task.Run(async () => {
                await RemoveItemsFromContainer<CompanyBranchModel>(
                    identityDatabase,
                    IdentityDatabaseAndContainerNames.CompanyBranchContainerName,
                    x => x.id,
                    x =>  new PartitionKeyBuilder().Add(x.BranchID).Add(x.ItemType).Build()
                );
            }),

            Task.Run(async () => {
                await RemoveItemsFromContainer<CountryModel>(
                    identityDatabase,
                    IdentityDatabaseAndContainerNames.CountryContainerName,
                    x => x.id,
                    x =>  {
                        var builder = new PartitionKeyBuilder();

                        if(x.CountryID.HasValue)
                            builder.Add(x.CountryID.Value);
                        else
                            builder.Add(null);

                        if(x.RegionID.HasValue)
                            builder.Add(x.RegionID.Value);
                        else
                            builder.Add(null);

                        builder.Add(x.ItemType);

                        return builder.Build();
                    }
                );
            }),

            Task.Run(async () => {
                await RemoveItemsFromContainer<DepartmentModel>(
                    identityDatabase,
                    IdentityDatabaseAndContainerNames.DepartmentContainerName,
                    x => x.id,
                    x =>  new PartitionKeyBuilder().Add(x.id).Build()
                );
            }),

            Task.Run(async () => {
                await RemoveItemsFromContainer<ServiceModel>(
                    identityDatabase,
                    IdentityDatabaseAndContainerNames.ServiceContainerName,
                    x => x.id,
                    x =>  new PartitionKeyBuilder().Add(x.id).Build()
                );
            }),

            Task.Run(async () => {
                await RemoveItemsFromContainer<TeamModel>(
                    identityDatabase,
                    IdentityDatabaseAndContainerNames.TeamContainerName,
                    x => x.id,
                    x =>  new PartitionKeyBuilder().Add(x.id).Build()
                );
            }),
        };

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// One-shot full backfill of every identity entity into Cosmos.
    /// <para>
    /// No <c>Replicate</c> call here passes a mapping delegate: each document is mapped through the ShiftMapper
    /// mapper that <c>AddShiftIdentityDashboard&lt;DB&gt;()</c> in <c>Program.cs</c> registers — the same
    /// <c>IdentityReplicationProfile</c> the save trigger uses, so a backfilled document is byte-identical to one
    /// a live save produces. The pipeline resolves and pair-checks that mapper BEFORE touching any row, so a host
    /// with no usable mapper gets an exception out of <c>RunAsync</c>, not permanently-dirty rows under a
    /// clean-looking watermark (which is how the old AutoMapper fallback failed).
    /// </para>
    /// <para>
    /// Copy this shape for your own entities: declare <c>CreateMap&lt;YourEntity, YourModel&gt;()</c> in a mapper (or
    /// a profile your mapper adds) and leave the delegate out — or pass one explicitly when a document needs
    /// something a map cannot say.
    /// </para>
    /// </summary>
    public static async Task ReplicateAll(CosmosDBReplication replication, CosmosClient client, string databaseId)
    {
        await replication.SetUp<DB, ShiftSoftware.ShiftIdentity.Data.Entities.Team>(client, databaseId)
            .Replicate<TeamModel>(IdentityDatabaseAndContainerNames.TeamContainerName)
            .RunAsync(true);

        await replication.SetUp<DB, ShiftSoftware.ShiftIdentity.Data.Entities.Country>(client, databaseId, x => x.Include(x => x.Regions))
            .Replicate<CountryModel>(IdentityDatabaseAndContainerNames.CountryContainerName)
            .RunAsync(true);

        await replication.SetUp<DB, ShiftSoftware.ShiftIdentity.Data.Entities.Region>(client, databaseId)
            .Replicate<RegionModel>(IdentityDatabaseAndContainerNames.CountryContainerName)
            .RunAsync(true);

        await replication.SetUp<DB, ShiftSoftware.ShiftIdentity.Data.Entities.City>(client, databaseId)
            .Replicate<CityModel>(IdentityDatabaseAndContainerNames.CountryContainerName)
            .RunAsync(true);

        await replication.SetUp<DB, ShiftSoftware.ShiftIdentity.Data.Entities.Company>(client, databaseId)
            .Replicate<CompanyModel>(IdentityDatabaseAndContainerNames.CompanyContainerName)
            .RunAsync(true);

        await replication.SetUp<DB, ShiftSoftware.ShiftIdentity.Data.Entities.CompanyBranch>(client, databaseId,
            q => q.Include(x => x.City).ThenInclude(x => x.Region).Include(x => x.Company))
            .Replicate<CompanyBranchModel>(IdentityDatabaseAndContainerNames.CompanyBranchContainerName)
            .RunAsync(true);

        await replication.SetUp<DB, ShiftSoftware.ShiftIdentity.Data.Entities.CompanyBranchDepartment>(client, databaseId, x => x.Include(i => i.Department))
            .Replicate<CompanyBranchSubItemModel>(IdentityDatabaseAndContainerNames.CompanyBranchContainerName)
            .RunAsync(true);

        await replication.SetUp<DB, ShiftSoftware.ShiftIdentity.Data.Entities.CompanyBranchService>(client, databaseId, x => x.Include(i => i.Service))
            .Replicate<CompanyBranchSubItemModel>(IdentityDatabaseAndContainerNames.CompanyBranchContainerName)
            .RunAsync(true);

        await replication.SetUp<DB, ShiftSoftware.ShiftIdentity.Data.Entities.CompanyBranchBrand>(client, databaseId, x => x.Include(i => i.Brand))
            .Replicate<CompanyBranchSubItemModel>(IdentityDatabaseAndContainerNames.CompanyBranchContainerName)
            .RunAsync(true);

        await replication.SetUp<DB, ShiftSoftware.ShiftIdentity.Data.Entities.Department>(client, databaseId)
            .Replicate<DepartmentModel>(IdentityDatabaseAndContainerNames.DepartmentContainerName)
            .RunAsync(true);

        await replication.SetUp<DB, ShiftSoftware.ShiftIdentity.Data.Entities.Service>(client, databaseId)
            .Replicate<ServiceModel>(IdentityDatabaseAndContainerNames.ServiceContainerName)
            .RunAsync(true);

        await replication.SetUp<DB, ShiftSoftware.ShiftIdentity.Data.Entities.Brand>(client, databaseId)
            .Replicate<BrandModel>(IdentityDatabaseAndContainerNames.BrandContainerName)
            .RunAsync(true);
    }
}
