using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ShiftIdentity.AspNetCore.Models;
using ShiftSoftware.ShiftIdentity.AspNetCore;
using ShiftSoftware.ShiftIdentity.Core;
using StockPlusPlus.Data;
using ShiftSoftware.ShiftIdentity.Dashboard.AspNetCore.Extentsions;
using ShiftSoftware.ShiftIdentity.AspNetCore.Extensions;
using ShiftSoftware.ShiftEntity.Web.Services;
using Microsoft.Extensions.Azure;
using ShiftSoftware.TypeAuth.AspNetCore.Extensions;
using System.Globalization;
using Microsoft.AspNetCore.Localization;
using AutoMapper;
using ShiftSoftware.ShiftIdentity.Core.Entities;
using ShiftSoftware.ShiftIdentity.Core.ReplicationModels;
using ShiftSoftware.ShiftEntity.Model.Enums;

var builder = WebApplication.CreateBuilder(args);

Action<DbContextOptionsBuilder> dbOptionBuilder = x =>
{
    x.UseSqlServer(builder.Configuration.GetConnectionString("SQLServer")!)
    .UseTemporal(true);
};

builder.Services.RegisterShiftRepositories(typeof(StockPlusPlus.Data.Marker).Assembly);

builder.Services.AddDbContext<DB>(dbOptionBuilder);

var cosmosConnectionString = builder.Configuration.GetValue<string>("CosmosDb:ConnectionString")!;

if (builder.Configuration.GetValue<bool>("CosmosDb:Enabled"))
{
    builder.Services.AddShiftEntityCosmosDbReplicationTrigger(x =>
    {
        string databaseId = "test";

        x.SetUpReplication<DB, Service>(cosmosConnectionString, databaseId, null, false)
            .Replicate("Services",
            x => x.id,
            e =>
            {
                var mapper = e.Services.GetRequiredService<IMapper>();
                return mapper.Map<ServiceModel>(e.Entity);
            })
            .UpdateReference<CompanyBranchServiceModel>("CompanyBranches",
                (q, e) => q.Where(x => x.id == e.Entity.ID.ToString() && x.ItemType == "Service"));

        x.SetUpReplication<DB, Region>(cosmosConnectionString, databaseId)
            .Replicate<RegionModel>("Regions", x => x.id, x => x.RegionID, x => x.ItemType)
            .UpdatePropertyReference<RegionModel, CompanyBranchModel>("CompanyBranches", x => x.City.Region,
            (q, e) => q.Where(x => x.City.Region.id == e.Entity.ID.ToString() && x.ItemType == "Branch"));

        x.SetUpReplication<DB, City>(cosmosConnectionString, databaseId)
            .Replicate<CityModel>("Regions", x => x.id, x => x.RegionID, x => x.ItemType,
            e =>
            {
                var mapper = e.Services.GetRequiredService<IMapper>();
                return mapper.Map<CityModel>(e.Entity);
            })
            .UpdatePropertyReference<CityCompanyBranchModel, CompanyBranchModel>("CompanyBranches", x => x.City,
            (q, e) => q.Where(x => x.City.id == e.Entity.ID.ToString() && x.ItemType == "Branch"));

        x.SetUpReplication<DB, CompanyBranch>(cosmosConnectionString, databaseId)
            .Replicate<CompanyBranchModel>("CompanyBranches", x => x.id, x => x.BranchID, x => x.ItemType);

        x.SetUpReplication<DB, Company>(cosmosConnectionString, databaseId)
            .Replicate<CompanyModel>("Companies", x=> x.id)
            .UpdatePropertyReference<CompanyModel, CompanyBranchModel>("CompanyBranches", x => x.Company,
            (q, e) => q.Where(x => x.Company.id == e.Entity.ID.ToString()));

        x.SetUpReplication<DB, CompanyBranchService>(cosmosConnectionString, databaseId)
            .Replicate<CompanyBranchServiceModel>("CompanyBranches", x => x.id, x => x.BranchID, x => x.ItemType);
    });
}

var mvcBuilder = builder.Services
    .AddLocalization()
    .AddHttpContextAccessor()
    .AddControllers();

builder.Services.AddShiftEntityPrint(x =>
{
    x.TokenExpirationInSeconds = 600;
    x.SASTokenKey = "One-Two-Three";
});

mvcBuilder.AddShiftEntityWeb(x =>
{
    x.WrapValidationErrorResponseWithShiftEntityResponse(true);
    x.AddAutoMapper(typeof(StockPlusPlus.Data.Marker).Assembly);

    x.HashId.RegisterHashId(builder.Configuration.GetValue<bool>("Settings:HashIdSettings:AcceptUnencodedIds"));
    x.HashId.RegisterIdentityHashId("one-two", 5);

    var azureStorageAccounts = new List<ShiftSoftware.ShiftEntity.Core.Services.AzureStorageOption>();

    builder.Configuration.Bind("AzureStorageAccounts", azureStorageAccounts);

    x.AddAzureStorage(azureStorageAccounts.ToArray());

    x.AddShiftIdentityAutoMapper();
});

mvcBuilder.AddShiftIdentity(builder.Configuration.GetValue<string>("Settings:TokenSettings:Issuer")!, builder.Configuration.GetValue<string>("Settings:TokenSettings:Key")!);

mvcBuilder.AddShiftIdentityDashboard<DB>(
    new ShiftIdentityConfiguration
    {
        ShiftIdentityHostingType = ShiftIdentityHostingTypes.Internal,
        Token = new TokenSettingsModel
        {
            ExpireSeconds = 60000,
            Issuer = builder.Configuration.GetValue<string>("Settings:TokenSettings:Issuer")!,
            Key = builder.Configuration.GetValue<string>("Settings:TokenSettings:Key")!,
        },
        Security = new SecuritySettingsModel
        {
            LockDownInMinutes = 0,
            LoginAttemptsForLockDown = 1000000,
            RequirePasswordChange = false
        },
        RefreshToken = new TokenSettingsModel
        {
            Audience = "stock-plus-plus",
            ExpireSeconds = 100000,
            Issuer = builder.Configuration.GetValue<string>("Settings:TokenSettings:Issuer")!,
            Key = builder.Configuration.GetValue<string>("Settings:TokenSettings:Key")!,
        },
        HashIdSettings = new HashIdSettings
        {
            AcceptUnencodedIds = true,
            UserIdsSalt = "k02iUHSb2ier9fiui02349AbfJEI",
            UserIdsMinHashLength = 5
        },
    }
);

mvcBuilder.AddShiftEntityOdata(x =>
{
    x.DefaultOptions();
    x.RegisterAllDTOs(typeof(StockPlusPlus.Shared.Marker).Assembly);
    x.RegisterShiftIdentityDashboardEntitySets();
});

builder.Services.AddSwaggerGen(c =>
{
    c.DocInclusionPredicate(SwaggerService.DocInclusionPredicate);
});

builder.Services.AddTypeAuth((o) =>
{
    o.AddActionTree<ShiftIdentityActions>();
#if (includeSampleApp)
    o.AddActionTree<StockPlusPlus.Shared.ActionTrees.SystemActionTrees>();
    o.AddActionTree<StockPlusPlus.Shared.ActionTrees.StockActionTrees>();
#endif
});

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddRazorPages();
    builder.Services.AddAzureClients(clientBuilder =>
    {
        clientBuilder.AddBlobServiceClient(builder.Configuration["StorageConnectionString:blob"]!, preferMsi: true);
        clientBuilder.AddQueueServiceClient(builder.Configuration["StorageConnectionString:queue"]!, preferMsi: true);
    });
}

var app = builder.Build();

//app.AddFakeIdentityEndPoints();

if (builder.Environment.IsDevelopment())
{
    var scope = app.Services.CreateScope();

    var db = scope.ServiceProvider.GetRequiredService<DB>();

    await db.Database.EnsureCreatedAsync();
}

if (app.Environment.EnvironmentName != "Test")
{
    await app.SeedDBAsync("SuperUser", "OneTwo", new ShiftSoftware.ShiftIdentity.Data.DBSeedOptions
    {
        RegionExternalId = "1",
        RegionShortCode = "KRG",

        CompanyShortCode = "SFT",
        CompanyExternalId = "-1",
        CompanyAlternativeExternalId = "shift-software",
        CompanyType = CompanyTypes.NotSpecified,

        CompanyBranchExternalId = "-11",
        CompanyBranchShortCode = "SFT-EBL"
    });

}

var supportedCultures = new List<CultureInfo>
{
    new CultureInfo("en-US"),
    new CultureInfo("ar-IQ"),
    new CultureInfo("ku-IQ"),
};

app.UseRequestLocalization(options =>
{
    options.SetDefaultCulture("en-US");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
    options.RequestCultureProviders = new List<IRequestCultureProvider> { new AcceptLanguageHeaderRequestCultureProvider() };
    options.ApplyCurrentCultureToResponseHeaders = true;
});

app.MapControllers();

app.UseCors(x => x.WithOrigins("*").AllowAnyMethod().AllowAnyHeader());

if (app.Environment.IsDevelopment())
{
    app.UseBlazorFrameworkFiles();
    app.UseStaticFiles();

    app.UseSwagger();
    app.UseSwaggerUI();

    app.MapRazorPages();
    app.MapFallbackToFile("index.html");
}

app.Run();