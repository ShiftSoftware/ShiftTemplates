using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ShiftIdentity.Core;
using StockPlusPlus.Data.DbContext;
using ShiftSoftware.ShiftIdentity.AspNetCore.Extensions;
using ShiftSoftware.ShiftEntity.Web.Services;
using ShiftSoftware.TypeAuth.AspNetCore.Extensions;
using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.Azure.Cosmos;
#if (includeSampleApp)
using StockPlusPlus.Shared.DTOs.Service;
#endif

#if (internalShiftIdentityHosting)
using StockPlusPlus.API.Services;
using AutoMapper;
using ShiftSoftware.ShiftIdentity.Dashboard.AspNetCore.Extentsions;
using ShiftSoftware.ShiftIdentity.Core.ReplicationModels;
using ShiftSoftware.ShiftIdentity.Core.Entities;
using ShiftSoftware.ShiftIdentity.AspNetCore.Models;
using ShiftSoftware.ShiftIdentity.AspNetCore;
using ShiftSoftware.ShiftEntity.Model.Enums;
using StockPlusPlus.Data.DbContext;
#endif
#if (externalShiftIdentityHosting)
using ShiftSoftware.ShiftIdentity.AspNetCore.Models;
#endif

var builder = WebApplication.CreateBuilder(args);

Action<DbContextOptionsBuilder> dbOptionBuilder = x =>
{
    x.UseSqlServer(builder.Configuration.GetConnectionString("SQLServer")!)
    .UseTemporal(true);
};

builder.Services.RegisterShiftRepositories(typeof(StockPlusPlus.Data.Marker).Assembly);

builder.Services.AddDbContext<DB>(dbOptionBuilder);

var cosmosConnectionString = builder.Configuration.GetValue<string>("CosmosDb:ConnectionString")!;
if(!string.IsNullOrWhiteSpace(cosmosConnectionString))
    builder.Services.AddSingleton(new CosmosClient(cosmosConnectionString));


if (builder.Configuration.GetValue<bool>("CosmosDb:Enabled"))
{
#if (internalShiftIdentityHosting)
    builder.Services.AddShiftEntityCosmosDbReplicationTrigger(x =>
    {
        string databaseId = "test";
        var client = x.Services.GetRequiredService<CosmosClient>();

        x.SetUpReplication<DB, Service>(client, databaseId, null, false)
            .Replicate<ServiceModel>(ReplicationConfiguration.ServiceContainerName, x => x.id)
            .UpdateReference<CompanyBranchSubItemModel>(ReplicationConfiguration.CompanyBranchContainerName,
                (q, e) => q.Where(x => x.ItemType == CompanyBranchContainerItemTypes.Service && x.id == e.Entity.ID.ToString()));

        x.SetUpReplication<DB, CompanyBranchService>(client, databaseId, null, false)
            .Replicate<CompanyBranchSubItemModel>(ReplicationConfiguration.CompanyBranchContainerName, x => x.BranchID, x => x.ItemType);

        x.SetUpReplication<DB, CompanyBranchDepartment>(client, databaseId, null, false)
            .Replicate<CompanyBranchSubItemModel>(ReplicationConfiguration.CompanyBranchContainerName, x => x.BranchID, x => x.ItemType);

        x.SetUpReplication<DB, Department>(client, databaseId, null, false)
            .Replicate<DepartmentModel>(ReplicationConfiguration.DepartmentContainerName, x => x.id)
            .UpdateReference<CompanyBranchSubItemModel>(ReplicationConfiguration.CompanyBranchContainerName,
                (q, e) => q.Where(x => x.ItemType == CompanyBranchContainerItemTypes.Department && x.id == e.Entity.ID.ToString()));

        x.SetUpReplication<DB, Brand>(client, databaseId, null, false)
            .Replicate<BrandModel>("Brands", x => x.id)
            .UpdateReference<CompanyBranchSubItemModel>(ReplicationConfiguration.CompanyBranchContainerName,
                (q, e) => q.Where(x => x.id == e.Entity.ID.ToString() && x.ItemType == CompanyBranchContainerItemTypes.Brand));

        x.SetUpReplication<DB, CompanyBranchBrand>(client, databaseId)
            .Replicate<CompanyBranchSubItemModel>(ReplicationConfiguration.CompanyBranchContainerName, x => x.BranchID, x => x.ItemType);

        x.SetUpReplication<DB, Region>(client, databaseId)
            .Replicate<RegionModel>("Regions", x => x.id, x => x.RegionID, x => x.ItemType)
            .UpdatePropertyReference<RegionModel, CompanyBranchModel>("CompanyBranches", x => x.City.Region,
            (q, e) => q.Where(x => x.City.Region.id == e.Entity.ID.ToString() && x.ItemType == "Branch"));

        x.SetUpReplication<DB, City>(client, databaseId)
            .Replicate<CityModel>("Regions", x => x.id, x => x.RegionID, x => x.ItemType,
            e =>
            {
                var mapper = e.Services.GetRequiredService<IMapper>();
                return mapper.Map<CityModel>(e.Entity);
            })
            .UpdatePropertyReference<CityCompanyBranchModel, CompanyBranchModel>("CompanyBranches", x => x.City,
            (q, e) => q.Where(x => x.City.id == e.Entity.ID.ToString() && x.ItemType == "Branch"));

        x.SetUpReplication<DB, CompanyBranch>(client, databaseId)
            .Replicate<CompanyBranchModel>("CompanyBranches", x => x.id, x => x.BranchID, x => x.ItemType);

        x.SetUpReplication<DB, Company>(client, databaseId)
            .Replicate<CompanyModel>("Companies", x=> x.id)
            .UpdatePropertyReference<CompanyModel, CompanyBranchModel>("CompanyBranches", x => x.Company,
            (q, e) => q.Where(x => x.Company.id == e.Entity.ID.ToString()));
    });
#endif
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
#if (internalShiftIdentityHosting)
    x.AddShiftIdentityAutoMapper();
#endif
});

mvcBuilder.AddShiftIdentity(builder.Configuration.GetValue<string>("Settings:TokenSettings:Issuer")!, builder.Configuration.GetValue<string>("Settings:TokenSettings:Key")!);

#if (internalShiftIdentityHosting)

builder.Services.AddScoped<ISendEmailVerification, SendEmailService>();
builder.Services.AddScoped<ISendEmailResetPassword, SendEmailService>();

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
        SASToken= new SASTokenModel
        {
            ExpiresInSeconds = 3600,
            Key = "One-Two-Three-Four-Five",
        },
        ShiftIdentityFeatureLocking = new ShiftIdentityFeatureLocking
        {
            RegionFeatureIsLocked = false,
            CityFeatureIsLocked = false,
            BrandFeatureIsLocked = false,
            DepartmentFeatureIsLocked = false,
            ServiceFeatureIsLocked = false,
            CompanyFeatureIsLocked = false,
            CompanyBranchFeatureIsLocked = false,
            AppFeatureIsLocked = false,
            AccessTreeFeatureIsLocked = false,
            UserFeatureIsLocked = false,
            TeamFeatureIsLocked = false,
        }
    }
);
#endif
mvcBuilder.AddShiftEntityOdata(x =>
{
    x.DefaultOptions();
    x.RegisterAllDTOs(typeof(StockPlusPlus.Shared.Marker).Assembly);
    #if (includeSampleApp)
    x.OdataEntitySet<ServiceListDTO>("Service");
    #endif
#if (internalShiftIdentityHosting)
    x.RegisterShiftIdentityDashboardEntitySets();
#endif
});

#if (externalShiftIdentityHosting)
if (builder.Environment.IsDevelopment())
{
    mvcBuilder.AddFakeIdentityEndPoints(
        new TokenSettingsModel
        {
            Issuer = builder.Configuration.GetValue<string>("Settings:TokenSettings:Issuer")!,
            Key = builder.Configuration.GetValue<string>("Settings:TokenSettings:Key")!,
            ExpireSeconds = 10000000
        }, new ShiftSoftware.ShiftIdentity.Core.DTOs.TokenUserDataDTO
        {
            FullName = "Test",
            ID = "1",
            Username = "test"
        },
        new ShiftSoftware.ShiftIdentity.Core.DTOs.App.AppDTO
        {
            AppId = "StockPlusPlus-Dev",
            DisplayName = "StockPlusPlus Dev",
            RedirectUri = "http://localhost:5069/Auth/Token"
        },
        "OneTwo",
#if (includeSampleApp)
        new string[]
         {
        """
            {
                "ShiftIdentityActions": ['r','w','d','m'],
                "SystemActionTrees": ['r','w','d','m'],
                "StockActionTrees": ['r','w','d','m']
            }
        """
         }
#else
new string[]
     {
        """
            {
                "ShiftIdentityActions": ['r','w','d','m']
            }
        """
     }
#endif
        );
}
#endif

builder.Services.AddSwaggerGen(c =>
{
    c.DocInclusionPredicate(SwaggerService.DocInclusionPredicate);
});

builder.Services.AddTypeAuth((o) =>
{
    o.AddActionTree<ShiftIdentityActions>();
#if (includeSampleApp)
    o.AddActionTree<StockPlusPlus.Shared.ActionTrees.SystemActionTrees>();
    o.AddActionTree<StockPlusPlus.Shared.ActionTrees.StockPlusPlusActionTree>();
#endif
});

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddRazorPages();
}

var app = builder.Build();

#if (externalShiftIdentityHosting)
app.AddFakeIdentityEndPoints();
#endif
//if (builder.Environment.IsDevelopment())
//{
//    var scope = app.Services.CreateScope();

//    var db = scope.ServiceProvider.GetRequiredService<DB>();

//    await db.Database.EnsureCreatedAsync();
//}

if (app.Environment.EnvironmentName != "Test")
{
#if (internalShiftIdentityHosting)
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
#endif
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