using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Azure.Cosmos;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using ShiftIdentity.Tests.Infrastructure;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.CosmosDbReplication.Services;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using ShiftSoftware.ShiftEntity.Model.Enums;
using ShiftSoftware.ShiftEntity.Model.Replication;
using ShiftSoftware.ShiftIdentity.Core.DTOs.CompanyBranch;
using ShiftSoftware.ShiftIdentity.Data.Entities;
using ShiftSoftware.ShiftIdentity.Data.Repositories;
using StockPlusPlus.API.Controllers;
using StockPlusPlus.Data.DbContext;
using Xunit;

namespace StockPlusPlus.Test.LongRunning;

/// <summary>
/// The sample host with its real Identity replication (<c>SetUpAllIdentityReplications</c>, the dashboard's
/// repositories and preparers, the replication mapper) and a Cosmos DB stand-in that records every write and keeps
/// the documents. It runs on a catalog of its own, which it creates and drops.
/// </summary>
public sealed class IdentityReplicationPinFactory : CustomWebApplicationFactory
{
    private readonly string connectionString;

    // CompanyBranches is declared so that a write or a query there would be recorded, not refused.
    public RecordingCosmos Cosmos { get; } = new RecordingCosmos(IdentityDatabaseAndContainerNames.DatabaseName)
        .Container(IdentityDatabaseAndContainerNames.CompanyBranchContainerName, "/BranchID", "/ItemType")
        .Container(IdentityDatabaseAndContainerNames.BranchContainerName, "/id")
        .Container(IdentityDatabaseAndContainerNames.CountryContainerName, "/CountryID", "/RegionID", "/ItemType")
        .Container(IdentityDatabaseAndContainerNames.CompanyContainerName, "/id")
        .Container(IdentityDatabaseAndContainerNames.ServiceContainerName, "/id")
        .Container(IdentityDatabaseAndContainerNames.DepartmentContainerName, "/id")
        .Container(IdentityDatabaseAndContainerNames.BrandContainerName, "/id")
        .Container(IdentityDatabaseAndContainerNames.TeamContainerName, "/id")
        .Container(IdentityDatabaseAndContainerNames.UserContainerName, "/id")
        .Container(IdentityDatabaseAndContainerNames.AccessTreeContainerName, "/id")
        .Container(IdentityDatabaseAndContainerNames.UserAccessTreeContainerName, "/id")
        .Container(IdentityDatabaseAndContainerNames.TeamUserContainerName, "/id");

    public ReplicationSyncLog Log { get; } = new();

    public IdentityReplicationPinFactory()
    {
        var configured = new SqlConnectionStringBuilder(base.CreateTestConfigurationBuilder().Build().GetConnectionString("SQLServer_Test"));

        // This factory deletes and recreates its catalog, so it only ever uses a local server and a catalog of its own.
        if (!Regex.IsMatch(configured.DataSource, @"^(?:\.|localhost|127\.0\.0\.1)(?:\\SQLEXPRESS)?(?:,\d+)?$", RegexOptions.IgnoreCase))
            throw new InvalidOperationException("The replication pins need a local SQL Server.");

        configured.InitialCatalog = "StockPlusPlus_ReplicationPins_" + Guid.NewGuid().ToString("N");
        connectionString = configured.ConnectionString;
    }

    protected override IConfigurationBuilder CreateTestConfigurationBuilder() => base.CreateTestConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["CosmosDb:Enabled"] = "true",
            ["ConnectionStrings:SQLServer_Test"] = connectionString,
        });

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<CosmosClient>();
            services.AddSingleton(Cosmos.Client);
            services.AddSingleton<ILoggerProvider>(Log);
        });
    }

    public override async ValueTask DisposeAsync()
    {
        await Log.AllFinishedAsync();
        await base.DisposeAsync();
        Cosmos.Dispose();

        var builder = new SqlConnectionStringBuilder(connectionString);
        var catalog = builder.InitialCatalog;
        SqlConnection.ClearPool(new SqlConnection(connectionString));
        builder.InitialCatalog = "master";
        await using var server = new SqlConnection(builder.ConnectionString);
        await server.OpenAsync();
        await using var drop = server.CreateCommand();
        drop.CommandText = $"IF DB_ID(N'{catalog}') IS NOT NULL BEGIN ALTER DATABASE [{catalog}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{catalog}] END";
        await drop.ExecuteNonQueryAsync();
    }
}

/// <summary>
/// Pins what Identity's replication writes for branches, through the dashboard's own save path: a branch saved with
/// its services, department and brand; the branch edited to drop a service; a service renamed and then deleted; the
/// city, region, country and company renamed; and the sample's forced backfill. For each step it pins the branch's
/// document in the <c>Branches</c> container: rebuilt from SQL whenever any row it is made of changes, with the city's
/// region and country and the company always filled, and without the dropped service or the deleted one. At the end it
/// pins the documents each container holds.
/// <para>
/// Nothing is written to the <c>CompanyBranches</c> container, or read from it, at any step. Until this release a
/// branch, its links and the reference updates of its parts were written there too.
/// </para>
/// </summary>
/// <remarks>
/// The rows of one save replicate in parallel, so the writes of one step are compared in sorted order. Audit dates
/// differ on every run, so they are compared as "date".
/// </remarks>
public class IdentityBranchReplicationPinTests(IdentityReplicationPinFactory factory) : IClassFixture<IdentityReplicationPinFactory>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static readonly Regex AuditDates = new("\"(CreateDate|LastSaveDate)\":\"[^\"]*\"");

    private static string Masked(string line) => AuditDates.Replace(line, "\"$1\":\"date\"");

    private readonly List<string> stored = new();
    private readonly List<string> branchSteps = new();
    private int seen;

    // Closes a step: the Branches writes made since the last step, sorted.
    private void Step(string name)
    {
        var writes = factory.Cosmos.Writes;
        branchSteps.Add("== " + name);
        branchSteps.AddRange(writes.Skip(seen)
            .Where(x => x.Container == IdentityDatabaseAndContainerNames.BranchContainerName)
            .Select(x => Masked(x.ToString()))
            .Order(StringComparer.Ordinal));
        seen = writes.Count;
    }

    private async Task SaveAsync(string step, Func<DB, Task> change)
    {
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DB>();
            await change(db);
            await db.SaveChangesAsync(Ct);
        }
        await factory.Log.AllFinishedAsync();
        Step(step);
    }

    private async Task SaveBranchAsync(string step, long? id, CompanyBranchDTO dto)
    {
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<CompanyBranchRepository>();
            // No signed-in user here, so the data-level checks are off. They decide who may save, not what replicates.
            var entity = id is null ? new CompanyBranch() : (await repository.FindAsync(id.Value, null, true, false))!;
            entity = await repository.UpsertAsync(entity, dto, id is null ? ActionTypes.Insert : ActionTypes.Update, null, null, true, false);
            if (id is null)
                repository.Add(entity);
            await repository.SaveChangesAsync();
        }
        await factory.Log.AllFinishedAsync();
        Step(step);
    }

    private static ShiftEntitySelectDTO Select(long id) => new() { Value = id.ToString() };

    [Fact]
    public async Task Branch_documents_as_the_dashboard_saves_them_today()
    {
        Country country = null!;
        Region region = null!;
        City city = null!;
        Company company = null!;
        Service wash = null!, paint = null!;
        Department sales = null!;
        Brand acme = null!;

        // The tree a branch hangs from, saved directly through the context.
        await SaveAsync("tree saved", db =>
        {
            country = new Country { Name = "Iraq", CallingCode = "+964" };
            region = new Region { Name = "Kurdistan", Country = country };
            city = new City { Name = "Erbil", Region = region };
            company = new Company { Name = "Shift" };
            wash = new Service { Name = "Wash" };
            paint = new Service { Name = "Paint" };
            sales = new Department { Name = "Sales" };
            acme = new Brand { Name = "Acme" };
            db.AddRange(country, region, city, company, wash, paint, sales, acme);
            return Task.CompletedTask;
        });

        var dto = new CompanyBranchDTO
        {
            Name = "Main",
            City = Select(city.ID),
            Company = Select(company.ID),
            Services = [Select(wash.ID), Select(paint.ID)],
            Departments = [Select(sales.ID)],
            Brands = [Select(acme.ID)],
        };
        await SaveBranchAsync("branch created with two services, a department and a brand", null, dto);

        long branchId;
        await using (var scope = factory.Services.CreateAsyncScope())
            branchId = await scope.ServiceProvider.GetRequiredService<DB>().CompanyBranches.Select(x => x.ID).SingleAsync(Ct);

        dto.Name = "Main Branch";
        dto.Services = [Select(wash.ID)];
        await SaveBranchAsync("branch renamed and the second service dropped", branchId, dto);

        await SaveAsync("service renamed", db => { db.Attach(wash); wash.Name = "Wash and Dry"; return Task.CompletedTask; });
        await SaveAsync("service deleted", db => { db.Attach(wash); wash.IsDeleted = true; return Task.CompletedTask; });
        await SaveAsync("city renamed", db => { db.Attach(city); city.Name = "Hawler"; return Task.CompletedTask; });
        await SaveAsync("region renamed", db => { db.Attach(region); region.Name = "KRG"; return Task.CompletedTask; });
        await SaveAsync("country renamed", db => { db.Attach(country); country.Name = "Republic of Iraq"; return Task.CompletedTask; });
        await SaveAsync("company renamed", db => { db.Attach(company); company.Name = "Shift Software"; return Task.CompletedTask; });

        // The sample's forced backfill, over the same rows.
        await using (var scope = factory.Services.CreateAsyncScope())
            await UtilityController.ReplicateAll(scope.ServiceProvider.GetRequiredService<CosmosDBReplication>(),
                factory.Cosmos.Client, IdentityDatabaseAndContainerNames.DatabaseName);
        Step("forced backfill");

        foreach (var container in new[]
        {
            IdentityDatabaseAndContainerNames.CompanyBranchContainerName, IdentityDatabaseAndContainerNames.CountryContainerName,
            IdentityDatabaseAndContainerNames.CompanyContainerName, IdentityDatabaseAndContainerNames.ServiceContainerName,
            IdentityDatabaseAndContainerNames.DepartmentContainerName, IdentityDatabaseAndContainerNames.BrandContainerName,
        })
        {
            stored.Add("== stored " + container);
            stored.AddRange(factory.Cosmos.StoredLines(container).Select(Masked));
        }

        branchSteps.Add("== stored " + IdentityDatabaseAndContainerNames.BranchContainerName);
        branchSteps.AddRange(factory.Cosmos.StoredLines(IdentityDatabaseAndContainerNames.BranchContainerName).Select(Masked));

        Assert.Empty(factory.Log.Failed);
        Assert.DoesNotContain(factory.Cosmos.Writes, x => x.Container == IdentityDatabaseAndContainerNames.CompanyBranchContainerName);
        Assert.DoesNotContain(factory.Cosmos.Queries, x => x.Container == IdentityDatabaseAndContainerNames.CompanyBranchContainerName);
        Assert.Equal(Stored.Split('\n').Select(x => x.TrimEnd('\r')), stored);
        Assert.Equal(Branches.Split('\n').Select(x => x.TrimEnd('\r')), branchSteps);
    }

    // The documents each container holds at the end. CompanyBranches holds none. The others are what they were while
    // CompanyBranches was still written.
    private const string Stored = """
        == stored CompanyBranches
        == stored Countries
        {"CountryID":1,"RegionID":1,"Name":"KRG","IntegrationId":null,"ShortCode":null,"IsProtected":false,"ItemType":"Region","Flag":null,"DisplayOrder":null,"id":"1","ID":"1","CreateDate":"date","LastSaveDate":"date","CreatedByUserID":null,"LastSavedByUserID":null,"IsDeleted":false}
        {"CountryID":1,"RegionID":null,"ItemType":"Country","Name":"Republic of Iraq","IntegrationId":null,"ShortCode":null,"CallingCode":"+964","IsProtected":false,"Flag":null,"DisplayOrder":null,"id":"1","ID":"1","CreateDate":"date","LastSaveDate":"date","CreatedByUserID":null,"LastSavedByUserID":null,"IsDeleted":false}
        {"Name":"Hawler","IntegrationId":null,"CountryID":null,"RegionID":1,"IsProtected":false,"ItemType":"City","CityID":1,"DisplayOrder":null,"id":"1","ID":"1","CreateDate":"date","LastSaveDate":"date","CreatedByUserID":null,"LastSavedByUserID":null,"IsDeleted":false}
        == stored Companies
        {"Name":"Shift Software","LegalName":null,"IntegrationId":null,"ShortCode":null,"CompanyType":0,"Logo":null,"HQPhone":null,"HQEmail":null,"HQAddress":null,"Website":null,"IsProtected":false,"TerminationDate":null,"CustomFields":{},"ParentCompanyID":null,"CompanyID":1,"DisplayOrder":null,"id":"1","ID":"1","CreateDate":"date","LastSaveDate":"date","CreatedByUserID":null,"LastSavedByUserID":null,"IsDeleted":false}
        == stored Services
        {"Name":"Wash and Dry","IntegrationId":null,"id":"1","ID":"1","CreateDate":"date","LastSaveDate":"date","CreatedByUserID":null,"LastSavedByUserID":null,"IsDeleted":true}
        {"Name":"Paint","IntegrationId":null,"id":"2","ID":"2","CreateDate":"date","LastSaveDate":"date","CreatedByUserID":null,"LastSavedByUserID":null,"IsDeleted":false}
        == stored Departments
        {"Name":"Sales","IntegrationId":null,"id":"1","ID":"1","CreateDate":"date","LastSaveDate":"date","CreatedByUserID":null,"LastSavedByUserID":null,"IsDeleted":false}
        == stored Brands
        {"Name":"Acme","IntegrationId":null,"BrandID":1,"id":"1","ID":"1","CreateDate":"date","LastSaveDate":"date","CreatedByUserID":null,"LastSavedByUserID":null,"IsDeleted":false}
        """;

    // The branch's document in the Branches container after each step, and the one stored at the end.
    private const string Branches = """
        == tree saved
        == branch created with two services, a department and a brand
        upsert Branches/1 {"Services":[{"ID":1,"IntegrationId":null,"Name":"Wash"},{"ID":2,"IntegrationId":null,"Name":"Paint"}],"Departments":[{"ID":1,"IntegrationId":null,"Name":"Sales"}],"Brands":[{"ID":1,"IntegrationId":null,"Name":"Acme"}],"Name":"Main","Phone":null,"Phones":[],"ShortPhone":null,"Email":null,"Emails":[],"Address":null,"IntegrationId":null,"ShortCode":null,"TerminationDate":null,"Location":null,"Photos":null,"MobilePhotos":null,"WorkingHours":null,"WorkingDays":null,"IsProtected":false,"City":{"Name":"Erbil","IntegrationId":null,"IsProtected":false,"DisplayOrder":null,"Region":{"CountryID":"1","RegionID":"1","Name":"Kurdistan","IntegrationId":null,"ShortCode":null,"IsProtected":false,"Flag":null,"DisplayOrder":null,"Country":{"CountryID":1,"RegionID":null,"ItemType":"Country","Name":"Iraq","IntegrationId":null,"ShortCode":null,"CallingCode":"+964","IsProtected":false,"Flag":null,"DisplayOrder":null,"id":"1","ID":"1","CreateDate":"date","LastSaveDate":"date","CreatedByUserID":null,"LastSavedByUserID":null,"IsDeleted":false},"id":"1","ID":"1","CreateDate":"date","LastSaveDate":"date","CreatedByUserID":null,"LastSavedByUserID":null,"IsDeleted":false},"id":"1","ID":"1","CreateDate":"date","LastSaveDate":"date","CreatedByUserID":null,"LastSavedByUserID":null,"IsDeleted":false},"Company":{"Name":"Shift","LegalName":null,"IntegrationId":null,"ShortCode":null,"CompanyType":0,"Logo":null,"HQPhone":null,"HQEmail":null,"HQAddress":null,"Website":null,"IsProtected":false,"TerminationDate":null,"CustomFields":{},"ParentCompanyID":null,"CompanyID":1,"DisplayOrder":null,"id":"1","ID":"1","CreateDate":"date","LastSaveDate":"date","CreatedByUserID":null,"LastSavedByUserID":null,"IsDeleted":false},"BranchID":"1","ItemType":"Branch","CustomFields":{},"RegionID":1,"CityID":1,"CompanyID":1,"CountryID":1,"CompanyBranchID":1,"DisplayOrder":null,"DisplayName":null,"Description":null,"WebsiteURL":null,"PublishTargets":[],"id":"1","ID":"1","CreateDate":"date","LastSaveDate":"date","CreatedByUserID":null,"LastSavedByUserID":null,"IsDeleted":false}
        == branch renamed and the second service dropped
        upsert Branches/1 {"Services":[{"ID":1,"IntegrationId":null,"Name":"Wash"}],"Departments":[{"ID":1,"IntegrationId":null,"Name":"Sales"}],"Brands":[{"ID":1,"IntegrationId":null,"Name":"Acme"}],"Name":"Main Branch","Phone":null,"Phones":[],"ShortPhone":null,"Email":null,"Emails":[],"Address":null,"IntegrationId":null,"ShortCode":null,"TerminationDate":null,"Location":null,"Photos":null,"MobilePhotos":null,"WorkingHours":null,"WorkingDays":null,"IsProtected":false,"City":{"Name":"Erbil","IntegrationId":null,"IsProtected":false,"DisplayOrder":null,"Region":{"CountryID":"1","RegionID":"1","Name":"Kurdistan","IntegrationId":null,"ShortCode":null,"IsProtected":false,"Flag":null,"DisplayOrder":null,"Country":{"CountryID":1,"RegionID":null,"ItemType":"Country","Name":"Iraq","IntegrationId":null,"ShortCode":null,"CallingCode":"+964","IsProtected":false,"Flag":null,"DisplayOrder":null,"id":"1","ID":"1","CreateDate":"date","LastSaveDate":"date","CreatedByUserID":null,"LastSavedByUserID":null,"IsDeleted":false},"id":"1","ID":"1","CreateDate":"date","LastSaveDate":"date","CreatedByUserID":null,"LastSavedByUserID":null,"IsDeleted":false},"id":"1","ID":"1","CreateDate":"date","LastSaveDate":"date","CreatedByUserID":null,"LastSavedByUserID":null,"IsDeleted":false},"Company":{"Name":"Shift","LegalName":null,"IntegrationId":null,"ShortCode":null,"CompanyType":0,"Logo":null,"HQPhone":null,"HQEmail":null,"HQAddress":null,"Website":null,"IsProtected":false,"TerminationDate":null,"CustomFields":{},"ParentCompanyID":null,"CompanyID":1,"DisplayOrder":null,"id":"1","ID":"1","CreateDate":"date","LastSaveDate":"date","CreatedByUserID":null,"LastSavedByUserID":null,"IsDeleted":false},"BranchID":"1","ItemType":"Branch","CustomFields":{},"RegionID":1,"CityID":1,"CompanyID":1,"CountryID":1,"CompanyBranchID":1,"DisplayOrder":null,"DisplayName":null,"Description":null,"WebsiteURL":null,"PublishTargets":[],"id":"1","ID":"1","CreateDate":"date","LastSaveDate":"date","CreatedByUserID":null,"LastSavedByUserID":null,"IsDeleted":false}
        == service renamed
        upsert Branches/1 {"Services":[{"ID":1,"IntegrationId":null,"Name":"Wash and Dry"}],"Departments":[{"ID":1,"IntegrationId":null,"Name":"Sales"}],"Brands":[{"ID":1,"IntegrationId":null,"Name":"Acme"}],"Name":"Main Branch","Phone":null,"Phones":[],"ShortPhone":null,"Email":null,"Emails":[],"Address":null,"IntegrationId":null,"ShortCode":null,"TerminationDate":null,"Location":null,"Photos":null,"MobilePhotos":null,"WorkingHours":null,"WorkingDays":null,"IsProtected":false,"City":{"Name":"Erbil","IntegrationId":null,"IsProtected":false,"DisplayOrder":null,"Region":{"CountryID":"1","RegionID":"1","Name":"Kurdistan","IntegrationId":null,"ShortCode":null,"IsProtected":false,"Flag":null,"DisplayOrder":null,"Country":{"CountryID":1,"RegionID":null,"ItemType":"Country","Name":"Iraq","IntegrationId":null,"ShortCode":null,"CallingCode":"+964","IsProtected":false,"Flag":null,"DisplayOrder":null,"id":"1","ID":"1","CreateDate":"date","LastSaveDate":"date","CreatedByUserID":null,"LastSavedByUserID":null,"IsDeleted":false},"id":"1","ID":"1","CreateDate":"date","LastSaveDate":"date","CreatedByUserID":null,"LastSavedByUserID":null,"IsDeleted":false},"id":"1","ID":"1","CreateDate":"date","LastSaveDate":"date","CreatedByUserID":null,"LastSavedByUserID":null,"IsDeleted":false},"Company":{"Name":"Shift","LegalName":null,"IntegrationId":null,"ShortCode":null,"CompanyType":0,"Logo":null,"HQPhone":null,"HQEmail":null,"HQAddress":null,"Website":null,"IsProtected":false,"TerminationDate":null,"CustomFields":{},"ParentCompanyID":null,"CompanyID":1,"DisplayOrder":null,"id":"1","ID":"1","CreateDate":"date","LastSaveDate":"date","CreatedByUserID":null,"LastSavedByUserID":null,"IsDeleted":false},"BranchID":"1","ItemType":"Branch","CustomFields":{},"RegionID":1,"CityID":1,"CompanyID":1,"CountryID":1,"CompanyBranchID":1,"DisplayOrder":null,"DisplayName":null,"Description":null,"WebsiteURL":null,"PublishTargets":[],"id":"1","ID":"1","CreateDate":"date","LastSaveDate":"date","CreatedByUserID":null,"LastSavedByUserID":null,"IsDeleted":false}
        == service deleted
        upsert Branches/1 {"Services":[],"Departments":[{"ID":1,"IntegrationId":null,"Name":"Sales"}],"Brands":[{"ID":1,"IntegrationId":null,"Name":"Acme"}],"Name":"Main Branch","Phone":null,"Phones":[],"ShortPhone":null,"Email":null,"Emails":[],"Address":null,"IntegrationId":null,"ShortCode":null,"TerminationDate":null,"Location":null,"Photos":null,"MobilePhotos":null,"WorkingHours":null,"WorkingDays":null,"IsProtected":false,"City":{"Name":"Erbil","IntegrationId":null,"IsProtected":false,"DisplayOrder":null,"Region":{"CountryID":"1","RegionID":"1","Name":"Kurdistan","IntegrationId":null,"ShortCode":null,"IsProtected":false,"Flag":null,"DisplayOrder":null,"Country":{"CountryID":1,"RegionID":null,"ItemType":"Country","Name":"Iraq","IntegrationId":null,"ShortCode":null,"CallingCode":"+964","IsProtected":false,"Flag":null,"DisplayOrder":null,"id":"1","ID":"1","CreateDate":"date","LastSaveDate":"date","CreatedByUserID":null,"LastSavedByUserID":null,"IsDeleted":false},"id":"1","ID":"1","CreateDate":"date","LastSaveDate":"date","CreatedByUserID":null,"LastSavedByUserID":null,"IsDeleted":false},"id":"1","ID":"1","CreateDate":"date","LastSaveDate":"date","CreatedByUserID":null,"LastSavedByUserID":null,"IsDeleted":false},"Company":{"Name":"Shift","LegalName":null,"IntegrationId":null,"ShortCode":null,"CompanyType":0,"Logo":null,"HQPhone":null,"HQEmail":null,"HQAddress":null,"Website":null,"IsProtected":false,"TerminationDate":null,"CustomFields":{},"ParentCompanyID":null,"CompanyID":1,"DisplayOrder":null,"id":"1","ID":"1","CreateDate":"date","LastSaveDate":"date","CreatedByUserID":null,"LastSavedByUserID":null,"IsDeleted":false},"BranchID":"1","ItemType":"Branch","CustomFields":{},"RegionID":1,"CityID":1,"CompanyID":1,"CountryID":1,"CompanyBranchID":1,"DisplayOrder":null,"DisplayName":null,"Description":null,"WebsiteURL":null,"PublishTargets":[],"id":"1","ID":"1","CreateDate":"date","LastSaveDate":"date","CreatedByUserID":null,"LastSavedByUserID":null,"IsDeleted":false}
        == city renamed
        upsert Branches/1 {"Services":[],"Departments":[{"ID":1,"IntegrationId":null,"Name":"Sales"}],"Brands":[{"ID":1,"IntegrationId":null,"Name":"Acme"}],"Name":"Main Branch","Phone":null,"Phones":[],"ShortPhone":null,"Email":null,"Emails":[],"Address":null,"IntegrationId":null,"ShortCode":null,"TerminationDate":null,"Location":null,"Photos":null,"MobilePhotos":null,"WorkingHours":null,"WorkingDays":null,"IsProtected":false,"City":{"Name":"Hawler","IntegrationId":null,"IsProtected":false,"DisplayOrder":null,"Region":{"CountryID":"1","RegionID":"1","Name":"Kurdistan","IntegrationId":null,"ShortCode":null,"IsProtected":false,"Flag":null,"DisplayOrder":null,"Country":{"CountryID":1,"RegionID":null,"ItemType":"Country","Name":"Iraq","IntegrationId":null,"ShortCode":null,"CallingCode":"+964","IsProtected":false,"Flag":null,"DisplayOrder":null,"id":"1","ID":"1","CreateDate":"date","LastSaveDate":"date","CreatedByUserID":null,"LastSavedByUserID":null,"IsDeleted":false},"id":"1","ID":"1","CreateDate":"date","LastSaveDate":"date","CreatedByUserID":null,"LastSavedByUserID":null,"IsDeleted":false},"id":"1","ID":"1","CreateDate":"date","LastSaveDate":"date","CreatedByUserID":null,"LastSavedByUserID":null,"IsDeleted":false},"Company":{"Name":"Shift","LegalName":null,"IntegrationId":null,"ShortCode":null,"CompanyType":0,"Logo":null,"HQPhone":null,"HQEmail":null,"HQAddress":null,"Website":null,"IsProtected":false,"TerminationDate":null,"CustomFields":{},"ParentCompanyID":null,"CompanyID":1,"DisplayOrder":null,"id":"1","ID":"1","CreateDate":"date","LastSaveDate":"date","CreatedByUserID":null,"LastSavedByUserID":null,"IsDeleted":false},"BranchID":"1","ItemType":"Branch","CustomFields":{},"RegionID":1,"CityID":1,"CompanyID":1,"CountryID":1,"CompanyBranchID":1,"DisplayOrder":null,"DisplayName":null,"Description":null,"WebsiteURL":null,"PublishTargets":[],"id":"1","ID":"1","CreateDate":"date","LastSaveDate":"date","CreatedByUserID":null,"LastSavedByUserID":null,"IsDeleted":false}
        == region renamed
        upsert Branches/1 {"Services":[],"Departments":[{"ID":1,"IntegrationId":null,"Name":"Sales"}],"Brands":[{"ID":1,"IntegrationId":null,"Name":"Acme"}],"Name":"Main Branch","Phone":null,"Phones":[],"ShortPhone":null,"Email":null,"Emails":[],"Address":null,"IntegrationId":null,"ShortCode":null,"TerminationDate":null,"Location":null,"Photos":null,"MobilePhotos":null,"WorkingHours":null,"WorkingDays":null,"IsProtected":false,"City":{"Name":"Hawler","IntegrationId":null,"IsProtected":false,"DisplayOrder":null,"Region":{"CountryID":"1","RegionID":"1","Name":"KRG","IntegrationId":null,"ShortCode":null,"IsProtected":false,"Flag":null,"DisplayOrder":null,"Country":{"CountryID":1,"RegionID":null,"ItemType":"Country","Name":"Iraq","IntegrationId":null,"ShortCode":null,"CallingCode":"+964","IsProtected":false,"Flag":null,"DisplayOrder":null,"id":"1","ID":"1","CreateDate":"date","LastSaveDate":"date","CreatedByUserID":null,"LastSavedByUserID":null,"IsDeleted":false},"id":"1","ID":"1","CreateDate":"date","LastSaveDate":"date","CreatedByUserID":null,"LastSavedByUserID":null,"IsDeleted":false},"id":"1","ID":"1","CreateDate":"date","LastSaveDate":"date","CreatedByUserID":null,"LastSavedByUserID":null,"IsDeleted":false},"Company":{"Name":"Shift","LegalName":null,"IntegrationId":null,"ShortCode":null,"CompanyType":0,"Logo":null,"HQPhone":null,"HQEmail":null,"HQAddress":null,"Website":null,"IsProtected":false,"TerminationDate":null,"CustomFields":{},"ParentCompanyID":null,"CompanyID":1,"DisplayOrder":null,"id":"1","ID":"1","CreateDate":"date","LastSaveDate":"date","CreatedByUserID":null,"LastSavedByUserID":null,"IsDeleted":false},"BranchID":"1","ItemType":"Branch","CustomFields":{},"RegionID":1,"CityID":1,"CompanyID":1,"CountryID":1,"CompanyBranchID":1,"DisplayOrder":null,"DisplayName":null,"Description":null,"WebsiteURL":null,"PublishTargets":[],"id":"1","ID":"1","CreateDate":"date","LastSaveDate":"date","CreatedByUserID":null,"LastSavedByUserID":null,"IsDeleted":false}
        == country renamed
        upsert Branches/1 {"Services":[],"Departments":[{"ID":1,"IntegrationId":null,"Name":"Sales"}],"Brands":[{"ID":1,"IntegrationId":null,"Name":"Acme"}],"Name":"Main Branch","Phone":null,"Phones":[],"ShortPhone":null,"Email":null,"Emails":[],"Address":null,"IntegrationId":null,"ShortCode":null,"TerminationDate":null,"Location":null,"Photos":null,"MobilePhotos":null,"WorkingHours":null,"WorkingDays":null,"IsProtected":false,"City":{"Name":"Hawler","IntegrationId":null,"IsProtected":false,"DisplayOrder":null,"Region":{"CountryID":"1","RegionID":"1","Name":"KRG","IntegrationId":null,"ShortCode":null,"IsProtected":false,"Flag":null,"DisplayOrder":null,"Country":{"CountryID":1,"RegionID":null,"ItemType":"Country","Name":"Republic of Iraq","IntegrationId":null,"ShortCode":null,"CallingCode":"+964","IsProtected":false,"Flag":null,"DisplayOrder":null,"id":"1","ID":"1","CreateDate":"date","LastSaveDate":"date","CreatedByUserID":null,"LastSavedByUserID":null,"IsDeleted":false},"id":"1","ID":"1","CreateDate":"date","LastSaveDate":"date","CreatedByUserID":null,"LastSavedByUserID":null,"IsDeleted":false},"id":"1","ID":"1","CreateDate":"date","LastSaveDate":"date","CreatedByUserID":null,"LastSavedByUserID":null,"IsDeleted":false},"Company":{"Name":"Shift","LegalName":null,"IntegrationId":null,"ShortCode":null,"CompanyType":0,"Logo":null,"HQPhone":null,"HQEmail":null,"HQAddress":null,"Website":null,"IsProtected":false,"TerminationDate":null,"CustomFields":{},"ParentCompanyID":null,"CompanyID":1,"DisplayOrder":null,"id":"1","ID":"1","CreateDate":"date","LastSaveDate":"date","CreatedByUserID":null,"LastSavedByUserID":null,"IsDeleted":false},"BranchID":"1","ItemType":"Branch","CustomFields":{},"RegionID":1,"CityID":1,"CompanyID":1,"CountryID":1,"CompanyBranchID":1,"DisplayOrder":null,"DisplayName":null,"Description":null,"WebsiteURL":null,"PublishTargets":[],"id":"1","ID":"1","CreateDate":"date","LastSaveDate":"date","CreatedByUserID":null,"LastSavedByUserID":null,"IsDeleted":false}
        == company renamed
        upsert Branches/1 {"Services":[],"Departments":[{"ID":1,"IntegrationId":null,"Name":"Sales"}],"Brands":[{"ID":1,"IntegrationId":null,"Name":"Acme"}],"Name":"Main Branch","Phone":null,"Phones":[],"ShortPhone":null,"Email":null,"Emails":[],"Address":null,"IntegrationId":null,"ShortCode":null,"TerminationDate":null,"Location":null,"Photos":null,"MobilePhotos":null,"WorkingHours":null,"WorkingDays":null,"IsProtected":false,"City":{"Name":"Hawler","IntegrationId":null,"IsProtected":false,"DisplayOrder":null,"Region":{"CountryID":"1","RegionID":"1","Name":"KRG","IntegrationId":null,"ShortCode":null,"IsProtected":false,"Flag":null,"DisplayOrder":null,"Country":{"CountryID":1,"RegionID":null,"ItemType":"Country","Name":"Republic of Iraq","IntegrationId":null,"ShortCode":null,"CallingCode":"+964","IsProtected":false,"Flag":null,"DisplayOrder":null,"id":"1","ID":"1","CreateDate":"date","LastSaveDate":"date","CreatedByUserID":null,"LastSavedByUserID":null,"IsDeleted":false},"id":"1","ID":"1","CreateDate":"date","LastSaveDate":"date","CreatedByUserID":null,"LastSavedByUserID":null,"IsDeleted":false},"id":"1","ID":"1","CreateDate":"date","LastSaveDate":"date","CreatedByUserID":null,"LastSavedByUserID":null,"IsDeleted":false},"Company":{"Name":"Shift Software","LegalName":null,"IntegrationId":null,"ShortCode":null,"CompanyType":0,"Logo":null,"HQPhone":null,"HQEmail":null,"HQAddress":null,"Website":null,"IsProtected":false,"TerminationDate":null,"CustomFields":{},"ParentCompanyID":null,"CompanyID":1,"DisplayOrder":null,"id":"1","ID":"1","CreateDate":"date","LastSaveDate":"date","CreatedByUserID":null,"LastSavedByUserID":null,"IsDeleted":false},"BranchID":"1","ItemType":"Branch","CustomFields":{},"RegionID":1,"CityID":1,"CompanyID":1,"CountryID":1,"CompanyBranchID":1,"DisplayOrder":null,"DisplayName":null,"Description":null,"WebsiteURL":null,"PublishTargets":[],"id":"1","ID":"1","CreateDate":"date","LastSaveDate":"date","CreatedByUserID":null,"LastSavedByUserID":null,"IsDeleted":false}
        == forced backfill
        upsert Branches/1 {"Services":[],"Departments":[{"ID":1,"IntegrationId":null,"Name":"Sales"}],"Brands":[{"ID":1,"IntegrationId":null,"Name":"Acme"}],"Name":"Main Branch","Phone":null,"Phones":[],"ShortPhone":null,"Email":null,"Emails":[],"Address":null,"IntegrationId":null,"ShortCode":null,"TerminationDate":null,"Location":null,"Photos":null,"MobilePhotos":null,"WorkingHours":null,"WorkingDays":null,"IsProtected":false,"City":{"Name":"Hawler","IntegrationId":null,"IsProtected":false,"DisplayOrder":null,"Region":{"CountryID":"1","RegionID":"1","Name":"KRG","IntegrationId":null,"ShortCode":null,"IsProtected":false,"Flag":null,"DisplayOrder":null,"Country":{"CountryID":1,"RegionID":null,"ItemType":"Country","Name":"Republic of Iraq","IntegrationId":null,"ShortCode":null,"CallingCode":"+964","IsProtected":false,"Flag":null,"DisplayOrder":null,"id":"1","ID":"1","CreateDate":"date","LastSaveDate":"date","CreatedByUserID":null,"LastSavedByUserID":null,"IsDeleted":false},"id":"1","ID":"1","CreateDate":"date","LastSaveDate":"date","CreatedByUserID":null,"LastSavedByUserID":null,"IsDeleted":false},"id":"1","ID":"1","CreateDate":"date","LastSaveDate":"date","CreatedByUserID":null,"LastSavedByUserID":null,"IsDeleted":false},"Company":{"Name":"Shift Software","LegalName":null,"IntegrationId":null,"ShortCode":null,"CompanyType":0,"Logo":null,"HQPhone":null,"HQEmail":null,"HQAddress":null,"Website":null,"IsProtected":false,"TerminationDate":null,"CustomFields":{},"ParentCompanyID":null,"CompanyID":1,"DisplayOrder":null,"id":"1","ID":"1","CreateDate":"date","LastSaveDate":"date","CreatedByUserID":null,"LastSavedByUserID":null,"IsDeleted":false},"BranchID":"1","ItemType":"Branch","CustomFields":{},"RegionID":1,"CityID":1,"CompanyID":1,"CountryID":1,"CompanyBranchID":1,"DisplayOrder":null,"DisplayName":null,"Description":null,"WebsiteURL":null,"PublishTargets":[],"id":"1","ID":"1","CreateDate":"date","LastSaveDate":"date","CreatedByUserID":null,"LastSavedByUserID":null,"IsDeleted":false}
        == stored Branches
        {"Services":[],"Departments":[{"ID":1,"IntegrationId":null,"Name":"Sales"}],"Brands":[{"ID":1,"IntegrationId":null,"Name":"Acme"}],"Name":"Main Branch","Phone":null,"Phones":[],"ShortPhone":null,"Email":null,"Emails":[],"Address":null,"IntegrationId":null,"ShortCode":null,"TerminationDate":null,"Location":null,"Photos":null,"MobilePhotos":null,"WorkingHours":null,"WorkingDays":null,"IsProtected":false,"City":{"Name":"Hawler","IntegrationId":null,"IsProtected":false,"DisplayOrder":null,"Region":{"CountryID":"1","RegionID":"1","Name":"KRG","IntegrationId":null,"ShortCode":null,"IsProtected":false,"Flag":null,"DisplayOrder":null,"Country":{"CountryID":1,"RegionID":null,"ItemType":"Country","Name":"Republic of Iraq","IntegrationId":null,"ShortCode":null,"CallingCode":"+964","IsProtected":false,"Flag":null,"DisplayOrder":null,"id":"1","ID":"1","CreateDate":"date","LastSaveDate":"date","CreatedByUserID":null,"LastSavedByUserID":null,"IsDeleted":false},"id":"1","ID":"1","CreateDate":"date","LastSaveDate":"date","CreatedByUserID":null,"LastSavedByUserID":null,"IsDeleted":false},"id":"1","ID":"1","CreateDate":"date","LastSaveDate":"date","CreatedByUserID":null,"LastSavedByUserID":null,"IsDeleted":false},"Company":{"Name":"Shift Software","LegalName":null,"IntegrationId":null,"ShortCode":null,"CompanyType":0,"Logo":null,"HQPhone":null,"HQEmail":null,"HQAddress":null,"Website":null,"IsProtected":false,"TerminationDate":null,"CustomFields":{},"ParentCompanyID":null,"CompanyID":1,"DisplayOrder":null,"id":"1","ID":"1","CreateDate":"date","LastSaveDate":"date","CreatedByUserID":null,"LastSavedByUserID":null,"IsDeleted":false},"BranchID":"1","ItemType":"Branch","CustomFields":{},"RegionID":1,"CityID":1,"CompanyID":1,"CountryID":1,"CompanyBranchID":1,"DisplayOrder":null,"DisplayName":null,"Description":null,"WebsiteURL":null,"PublishTargets":[],"id":"1","ID":"1","CreateDate":"date","LastSaveDate":"date","CreatedByUserID":null,"LastSavedByUserID":null,"IsDeleted":false}
        """;
}
