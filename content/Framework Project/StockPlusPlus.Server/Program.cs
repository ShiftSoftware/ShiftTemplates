using BitzArt.Blazor.Cookies;
using ShiftSoftware.ShiftIdentity.Blazor.Server.Extensions;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;
using ShiftSoftware.ShiftBlazor.Extensions;
using ShiftSoftware.ShiftBlazor.Services;
using ShiftSoftware.ShiftEntity.Core.Extensions;
using ShiftSoftware.ShiftEntity.Web.Services;
using ShiftSoftware.ShiftIdentity.AspNetCore.Models;
using ShiftSoftware.ShiftIdentity.AspNetCore.Services;
using ShiftSoftware.ShiftIdentity.Core;
using ShiftSoftware.ShiftIdentity.Core.DTOs;
using ShiftSoftware.ShiftIdentity.Dashboard.Blazor.Extensions;
using ShiftSoftware.TypeAuth.AspNetCore.Extensions;
using StockPlusPlus.Data.DbContext;
using StockPlusPlus.Server.Components;
using System.Globalization;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.Azure.Cosmos;
#if (includeSampleApp)
using StockPlusPlus.Shared.DTOs.Service;
using StockPlusPlus.Shared.DTOs.ProductBrand;
using StockPlusPlus.Shared.DTOs.ProductCategory;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using StockPlusPlus.Shared.ActionTrees;
using ShiftSoftware.ShiftIdentity.AspNetCore.Extensions;





#endif

#if (internalShiftIdentityHosting)
using StockPlusPlus.API.Services;
using AutoMapper;
using ShiftSoftware.ShiftIdentity.Dashboard.AspNetCore.Extentsions;
using ShiftSoftware.ShiftEntity.Model.Replication.IdentityModels;
using ShiftSoftware.ShiftIdentity.Core.Entities;
using ShiftSoftware.ShiftIdentity.AspNetCore.Models;
using ShiftSoftware.ShiftIdentity.AspNetCore;
using ShiftSoftware.ShiftEntity.Model.Enums;
using StockPlusPlus.Shared.Localization;
using ShiftSoftware.ShiftIdentity.Dashboard.AspNetCore;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Web.Explorer;
using ShiftSoftware.ShiftEntity.Model.Replication;
using Microsoft.AspNetCore.OData;
using ShiftSoftware.TypeAuth.Core;
using StockPlusPlus.Shared.DTOs.ProductCategory;
using StockPlusPlus.Shared.DTOs.ProductBrand;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using StockPlusPlus.Shared.ActionTrees;
using ShiftSoftware.ShiftIdentity.Dashboard.AspNetCore.Extensions;
#endif
#if (externalShiftIdentityHosting)
using ShiftSoftware.ShiftIdentity.AspNetCore.Models;
#endif

var builder = WebApplication.CreateBuilder(args);

#region Database & Data Layer

Action<DbContextOptionsBuilder> dbOptionBuilder = x =>
{
    x.UseSqlServer(builder.Configuration.GetConnectionString("SQLServer")!)
    .UseTemporal(true);
};

builder.Services.RegisterShiftRepositories(typeof(StockPlusPlus.Data.Marker).Assembly);
builder.Services.AddDbContext<DB>(dbOptionBuilder);
builder.Services.AddHttpClient();

var cosmosConnectionString = builder.Configuration.GetValue<string>("CosmosDb:ConnectionString")!;
if (!string.IsNullOrWhiteSpace(cosmosConnectionString))
    builder.Services.AddSingleton(new CosmosClient(cosmosConnectionString));

var IsCosmosEnabled = builder.Configuration.GetValue<bool>("CosmosDb:Enabled");

builder.Services.AddFileExplorer(x =>
{
    x.UseAzureBlobStorage();

    if (IsCosmosEnabled)
    {
        x.DatabaseId = builder.Configuration.GetValue<string>("CosmosDb:DefaultDatabaseName");
        x.ContainerId = builder.Configuration.GetValue<string>("CosmosDb:FileExplorerContainerId");
    }
});

if (IsCosmosEnabled)
{
#if (internalShiftIdentityHosting)

    var liveIdentitySQLServer = builder.Configuration.GetConnectionString("LiveIdentitySQLServer")!;
    if (!string.IsNullOrWhiteSpace(liveIdentitySQLServer))
        builder.Services.AddDbContext<LiveShiftIdentityDbContext>(options => options.UseSqlServer(liveIdentitySQLServer));

    builder.Services.AddShiftEntityCosmosDbReplicationTrigger<DB>(x =>
    {
        string databaseId = "Identity";
        var client = x.Services.GetRequiredService<CosmosClient>();

        x.SetUpReplication<DB, Service>(client, databaseId, null, false)
            .Replicate<ServiceModel>(IdentityDatabaseAndContainerNames.ServiceContainerName, x => x.id)
            .UpdateReference<CompanyBranchSubItemModel>(IdentityDatabaseAndContainerNames.CompanyBranchContainerName,
                (q, e) => q.Where(x => x.ItemType == CompanyBranchContainerItemTypes.Service && x.id == e.Entity.ID.ToString()));

        x.SetUpReplication<DB, CompanyBranchService>(client, databaseId, null, false)
            .Replicate<CompanyBranchSubItemModel>(IdentityDatabaseAndContainerNames.CompanyBranchContainerName, x => x.BranchID, x => x.ItemType);

        x.SetUpReplication<DB, CompanyBranchDepartment>(client, databaseId, null, false)
            .Replicate<CompanyBranchSubItemModel>(IdentityDatabaseAndContainerNames.CompanyBranchContainerName, x => x.BranchID, x => x.ItemType);

        x.SetUpReplication<DB, Department>(client, databaseId, null, false)
            .Replicate<DepartmentModel>(IdentityDatabaseAndContainerNames.DepartmentContainerName, x => x.id)
            .UpdateReference<CompanyBranchSubItemModel>(IdentityDatabaseAndContainerNames.CompanyBranchContainerName,
                (q, e) => q.Where(x => x.ItemType == CompanyBranchContainerItemTypes.Department && x.id == e.Entity.ID.ToString()));

        x.SetUpReplication<DB, Brand>(client, databaseId, null, false)
            .Replicate<BrandModel>("Brands", x => x.id)
            .UpdateReference<CompanyBranchSubItemModel>(IdentityDatabaseAndContainerNames.CompanyBranchContainerName,
                (q, e) => q.Where(x => x.id == e.Entity.ID.ToString() && x.ItemType == CompanyBranchContainerItemTypes.Brand));

        x.SetUpReplication<DB, CompanyBranchBrand>(client, databaseId)
            .Replicate<CompanyBranchSubItemModel>(IdentityDatabaseAndContainerNames.CompanyBranchContainerName, x => x.BranchID, x => x.ItemType);

        x.SetUpReplication<DB, Region>(client, databaseId)
            .Replicate<RegionModel>(IdentityDatabaseAndContainerNames.CountryContainerName, x => x.CountryID, x => x.RegionID, x => x.ItemType)
            .UpdatePropertyReference<CityRegionModel, CompanyBranchModel>("CompanyBranches", x => x.City.Region,
            (q, e) => q.Where(x => x.City.Region.id == e.Entity.ID.ToString() && x.ItemType == "Branch"));

        x.SetUpReplication<DB, Country>(client, databaseId)
            .Replicate<CountryModel>(IdentityDatabaseAndContainerNames.CountryContainerName, x => x.CountryID, x => x.RegionID, x => x.ItemType)
            .UpdatePropertyReference<CountryModel, CompanyBranchModel>("CompanyBranches", x => x.City.Region.Country,
            (q, e) => q.Where(x => x.City.Region.Country.id == e.Entity.ID.ToString() && x.ItemType == "Branch"));

        x.SetUpReplication<DB, City>(client, databaseId)
            .Replicate<CityModel>(IdentityDatabaseAndContainerNames.CountryContainerName, x => x.CountryID, x => x.RegionID, x => x.ItemType,
            e =>
            {
                var mapper = e.Services.GetRequiredService<IMapper>();
                return mapper.Map<CityModel>(e.Entity);
            })
            .UpdatePropertyReference<CityCompanyBranchModel, CompanyBranchModel>("CompanyBranches", x => x.City,
            (q, e) => q.Where(x => x.City.id == e.Entity.ID.ToString() && x.ItemType == "Branch"));

        x.SetUpReplication<DB, CompanyBranch>(client, databaseId)
            .Replicate<CompanyBranchModel>("CompanyBranches", x => x.BranchID, x => x.ItemType);

        x.SetUpReplication<DB, Company>(client, databaseId)
            .Replicate<CompanyModel>("Companies", x => x.id)
            .UpdatePropertyReference<CompanyModel, CompanyBranchModel>("CompanyBranches", x => x.Company,
            (q, e) => q.Where(x => x.Company.id == e.Entity.ID.ToString()));

        x.SetUpReplication<DB, Team>(client, databaseId)
            .Replicate<TeamModel>("Teams", x => x.id);
    });
#endif
}

#endregion

#region Blazor & Razor Components

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddLocalization();
// used for storing user settings
builder.AddBlazorCookies();

var baseUrl = builder.Configuration!.GetValue<string>("BaseURL");
var shiftIdentityApiURL = builder.Configuration.GetValue<string>("ShiftIdentityApi");
shiftIdentityApiURL ??= baseUrl;
var shiftIdentityFrontEndURL = builder.Configuration.GetValue<string>("ShiftIdentityFrontEnd");
shiftIdentityFrontEndURL ??= baseUrl;

// Consider using named clients with IHttpClientFactory.
// we inject the ServerHttpMessageHandler so that if we switch to
// named HttpClients we can use it with AddHttpMessageHandler
builder.Services.AddTransient<ShiftSoftware.ShiftIdentity.Blazor.Server.Services.ServerHttpMessageHandler>();
builder.Services.AddScoped(sp =>
{
    var handler = sp.GetRequiredService<ShiftSoftware.ShiftIdentity.Blazor.Server.Services.ServerHttpMessageHandler>();
    handler.InnerHandler = new HttpClientHandler();
    return new HttpClient(handler)
    {
        BaseAddress = new Uri(baseUrl!)
    };
});
builder.Services.AddShiftBlazor(config =>
{
    config.ShiftConfiguration = options =>
    {
        options.BaseAddress = baseUrl!;
        options.ExternalAddresses = new Dictionary<string, string?>
        {
            ["ShiftIdentityApi"] = shiftIdentityApiURL,
            ["StockPlusPlus"] = baseUrl
        };
        options.UserListEndpoint = shiftIdentityApiURL.AddUrlPath("IdentityPublicUser");
#if (internalShiftIdentityHosting)
        options.AdditionalAssemblies = new[] { typeof(StockPlusPlus.Client._Imports).Assembly, typeof(ShiftSoftware.ShiftIdentity.Dashboard.Blazor.ShiftIdentityDashboarBlazorMaker).Assembly };
#else
        options.AdditionalAssemblies = new[] { typeof(StockPlusPlus.Client._Imports).Assembly };
#endif
        options.AddLanguage("en-US", "English", false)
               .AddLanguage("ar-IQ", "Arabic", true)
               .AddLanguage("ku-IQ", "Kurdish", true);
    };
});

// ShiftIdentityLocalizer needed by Dashboard.Blazor components
builder.Services.AddTransient(x => new ShiftSoftware.ShiftIdentity.Core.Localization.ShiftIdentityLocalizer(
    x, typeof(ShiftSoftwareLocalization.Identity.Resource)));

#endregion

#region API Controllers & ShiftEntity

builder.Services.AddShiftEntityPrint(x =>
{
    x.TokenExpirationInSeconds = 600;
    x.SASTokenKey = "One-Two-Three";
});

builder.Services
    .AddControllers()
    .AddShiftEntityWeb(x =>
    {
        x.AddDataAssembly(typeof(StockPlusPlus.Data.Marker).Assembly);
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
    })
    .AddShiftIdentity(builder.Configuration.GetValue<string>("Settings:TokenSettings:Issuer")!,
        builder.Configuration.GetValue<string>("Settings:TokenSettings:PublicKey")!,
        setAsDefaultScheme: false);

#endregion



#region ShiftIdentity Dashboard

builder.Services.AddShiftIdentityDashboardBlazor(x =>
{
    x.LogoPath = "/img/shift-full.png";
    x.Title = "StockPlusPlus";
    x.UseCookieAuth = true;
#if (externalShiftIdentityHosting)
    x.ExternalIdentityApiUrl = shiftIdentityApiURL;
#endif

    x.DynamicTypeAuthActionExpander = async () =>
    {
#if (includeSampleApp)
        var httpService = builder.Services.BuildServiceProvider().GetRequiredService<HttpClient>();

        ODataDTO<ProductBrandListDTO>? brands = null!;
        ODataDTO<ProductCategoryListDTO>? categories = null!;

        await Task.WhenAll(new List<Task>
        {
            Task.Run(async () => { brands = await httpService.GetFromJsonAsync<ODataDTO<ProductBrandListDTO>>("ProductBrand"); }),
            Task.Run(async () => { categories = await httpService.GetFromJsonAsync<ODataDTO<ProductCategoryListDTO>>("ProductCategory"); })
        });

        StockPlusPlusActionTree.DataLevelAccess.ProductBrand.Expand(brands!.Value.Select(x => new KeyValuePair<string, string>(x.ID!, x.Name!)).ToList());

        StockPlusPlusActionTree.DataLevelAccess.ProductCategory.Expand(categories!.Value.Select(x => new KeyValuePair<string, string>(x.ID!, x.Name!)).ToList());
#endif
    };
});

#if (internalShiftIdentityHosting)

builder.Services.AddScoped<ISendEmailVerification, SendEmailService>();
builder.Services.AddScoped<ISendEmailResetPassword, SendEmailService>();

builder.Services.AddShiftIdentityDashboard<DB>(
    new ShiftIdentityConfiguration
    {
        ShiftIdentityHostingType = ShiftIdentityHostingTypes.Internal,
        Token = new TokenSettingsModel
        {
            ExpireSeconds = builder.Configuration.GetValue<int>("Settings:TokenSettings:TokenExpirySeconds")!,
            Issuer = builder.Configuration.GetValue<string>("Settings:TokenSettings:Issuer")!,
            RSAPrivateKeyBase64 = builder.Configuration.GetValue<string>("Settings:TokenSettings:PrivateKey")!,
        },
        Security = new SecuritySettingsModel
        {
            LockDownInMinutes = 0,
            LoginAttemptsForLockDown = 1000000,
            RequirePasswordChange = false
        },
        RefreshToken = new RefreshTokenSettingsModel
        {
            Audience = "stock-plus-plus",
            ExpireSeconds = 100000,
            Issuer = builder.Configuration.GetValue<string>("Settings:TokenSettings:Issuer")!,
            Key = builder.Configuration.GetValue<string>("Settings:TokenSettings:RefreshTokenKey")!,
        },
        HashIdSettings = new HashIdSettings
        {
            AcceptUnencodedIds = true,
            UserIdsSalt = "k02iUHSb2ier9fiui02349AbfJEI",
            UserIdsMinHashLength = 5
        },
        SASToken = new SASTokenModel
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
        },
        DefaultDataLevelAccessOptions = new ShiftIdentityDefaultDataLevelAccessOptions
        {
            DisableDefaultCountryFilter = true
        }
    }
);

#endif

#if (externalShiftIdentityHosting)
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddFakeIdentityEndPoints(
        new TokenSettingsModel
        {
            Issuer = builder.Configuration.GetValue<string>("Settings:TokenSettings:Issuer")!,
            RSAPrivateKeyBase64 = builder.Configuration.GetValue<string>("Settings:TokenSettings:PrivateKey")!,
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
            RedirectUri = "http://localhost:5069/"
        },
        "OneTwo",
#if (includeSampleApp)
        new string[]
         {
        """
            {
                "ShiftIdentityActions":[1,2,3,4],
                "SystemActionTrees":[1,2,3,4],
                "StockPlusPlusActionTree":[1,2,3,4]
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

#endregion


#region Cookie Authentication

builder.Services.AddShiftIdentityBlazorServer(options =>
{
    options.CookieName = ".StockPlusPlus.Auth";
#if (internalShiftIdentityHosting)
    options.HostingType = ShiftSoftware.ShiftIdentity.Core.ShiftIdentityHostingTypes.Internal;
#else
    options.HostingType = ShiftSoftware.ShiftIdentity.Core.ShiftIdentityHostingTypes.External;
    options.ExternalIdentityApiUrl = shiftIdentityApiURL;
#endif
    options.JwtIssuer = builder.Configuration.GetValue<string>("Settings:TokenSettings:Issuer")!;
    options.JwtPublicKeyBase64 = builder.Configuration.GetValue<string>("Settings:TokenSettings:PublicKey")!;
});

#endregion

#region TypeAuth

// Server-side TypeAuth (for API controllers and Blazor Server components)
// The Client project registers its own Blazor-side TypeAuth for WASM
builder.Services.AddTypeAuth((o) =>
{
    o.AddActionTree<ShiftIdentityActions>();
    o.AddActionTree<ShiftSoftware.ShiftEntity.Core.GeneralActionTree>();
    o.AddActionTree<ShiftSoftware.ShiftEntity.Core.AzureStorageActionTree>();
#if (includeSampleApp)
    o.AddActionTree<StockPlusPlus.Shared.ActionTrees.StockPlusPlusActionTree>();
#endif
});

#endregion

#region Azure Clients

builder.Services.AddAzureClients(clientBuilder =>
{
    clientBuilder.AddBlobServiceClient(builder.Configuration["devstoreaccount1:blobServiceUri"]!).WithName("devstoreaccount1");
    clientBuilder.AddQueueServiceClient(builder.Configuration["devstoreaccount1:queueServiceUri"]!).WithName("devstoreaccount1");
});

#endregion

var app = builder.Build();

#region Identity Seeding & Fake Endpoints

#if (externalShiftIdentityHosting)
if (builder.Environment.IsDevelopment())
{
    app.AddFakeIdentityEndPoints();
}
#endif

if (app.Environment.EnvironmentName != "Test")
{
#if (internalShiftIdentityHosting)
    await app.SeedDBAsync("SuperUser", "OneTwo", new ShiftSoftware.ShiftIdentity.Data.DBSeedOptions
    {
        CountryExternalId = "1",
        CountryShortCode = "IQ",
        CountryCallingCode = "+964",

        RegionExternalId = "1",
        RegionShortCode = "KRG",

        CompanyShortCode = "SFT",
        CompanyExternalId = "-1",
        CompanyAlternativeExternalId = "shift-software",
        CompanyType = CompanyTypes.NotSpecified,

        CompanyBranchExternalId = "-11",
        CompanyBranchShortCode = "SFT-EBL"
    });

    await app.SetFullAccessAsync("t1", "t3");
#endif
}

#endregion

#region Localization

using (var scope = app.Services.CreateScope())
{
    var settingManager = scope.ServiceProvider.GetRequiredService<SettingManager>();
    var supportedCultures = SettingManager.Configuration.Languages.Select(x => x.CultureName).ToArray();

    app.UseRequestLocalization(options =>
    {
        var cultures = supportedCultures.Select(x => settingManager.GetCulture(x)).ToList();
        var defaultCulture = cultures.FirstOrDefault() ?? settingManager.GetCulture(DefaultAppSetting.Language.CultureName);
        options.DefaultRequestCulture = new RequestCulture(defaultCulture);
        options.SupportedCultures = cultures;
        options.SupportedUICultures = cultures;

        options.RequestCultureProviders = [
                new QueryStringRequestCultureProvider(),
                new CookieRequestCultureProvider(),
                new AcceptLanguageHeaderRequestCultureProvider()
            ];
        options.ApplyCurrentCultureToResponseHeaders = true;
    });
}

#endregion

#region Middleware Pipeline

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

app.UseCors(x => x.WithOrigins("*").AllowAnyMethod().AllowAnyHeader());

app.MapControllers();

app.MapShiftIdentityCookieEndpoints();

app.MapStaticAssets();
app.MapRazorComponents<StockPlusPlus.Server.Components.App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(
        typeof(StockPlusPlus.Client._Imports).Assembly,
        typeof(ShiftSoftware.ShiftIdentity.Dashboard.Blazor.ShiftIdentityDashboarBlazorMaker).Assembly);

#endregion

app.Run();