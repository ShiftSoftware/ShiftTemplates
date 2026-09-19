using Microsoft.EntityFrameworkCore;
using StockPlusPlus.Data.DbContext;
using ShiftSoftware.ShiftEntity.Web.Services;
using ShiftSoftware.TypeAuth.AspNetCore.Extensions;
using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Azure;
using ShiftSoftware.ShiftEntity.Core.Attention;
using ShiftSoftware.ShiftIdentity.Core;
using ShiftSoftware.ShiftEntity.EFCore.Tagging;
using ShiftSoftware.ShiftEntity.Web.Attention;
using ShiftSoftware.ShiftEntity.Web.Tagging;
#if (includeSampleApp)
using StockPlusPlus.API.Endpoints;
using StockPlusPlus.Data.Evaluators;
using StockPlusPlus.Shared.ActionTrees;
using StockPlusPlus.Shared.DTOs.Service;
#endif

#if (internalShiftIdentityHosting)
using ShiftSoftware.ShiftIdentity.AspNetCore.Authentication;
using StockPlusPlus.API.Services;
using ShiftSoftware.ShiftIdentity.Dashboard.AspNetCore.Extentsions;
using ShiftSoftware.ShiftEntity.Model.Replication.IdentityModels;
using ShiftSoftware.ShiftIdentity.Data.Entities;
using ShiftSoftware.ShiftIdentity.Core.Models;
using ShiftSoftware.ShiftEntity.Model.Enums;
using StockPlusPlus.Shared.Localization;
using ShiftSoftware.ShiftIdentity.Dashboard.AspNetCore;
using ShiftSoftware.ShiftIdentity.Dashboard.AspNetCore.Replication;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Web.Explorer;
using ShiftSoftware.ShiftEntity.Model.Replication;
using Microsoft.AspNetCore.OData;
using ShiftSoftware.TypeAuth.Core;
#endif
#if (externalShiftIdentityHosting)
using ShiftSoftware.ShiftIdentity.Core.Models;
#endif

#if IDENTITY_DEVELOPMENT_APP
if (!args.Contains("--identity-development", StringComparer.Ordinal))
    throw new InvalidOperationException("This development build requires --identity-development. Rebuild normally for configured application startup.");
await StockPlusPlus.API.Development.IdentityApp.IdentityDevelopmentHost.RunAsync();
return;
#elif IDENTITY_ADMISSION_PREVIEW
if (!args.Contains("--identity-preview", StringComparer.Ordinal))
    throw new InvalidOperationException("This local preview build requires --identity-preview. Rebuild normally to run the application.");
await StockPlusPlus.API.Development.IdentityPreview.IdentityPreviewHost.RunAsync();
return;
#else
if (args.Contains("--identity-development", StringComparer.Ordinal))
    throw new InvalidOperationException("Build with eng/Start-IdentityDevelopmentApp.ps1 before using --identity-development.");
if (args.Contains("--identity-preview", StringComparer.Ordinal))
    throw new InvalidOperationException("Build the local admission preview with eng/Start-IdentityPreview.ps1 before using --identity-preview.");

var builder = WebApplication.CreateBuilder(args);

Action<DbContextOptionsBuilder> dbOptionBuilder = x =>
{
    x.UseSqlServer(builder.Configuration.GetConnectionString("SQLServer")!)
    .UseTemporal(true);
};

// Also wires attribute-driven endpoints: scans the given (data) assembly for entities decorated with
// [ShiftEntityEndpoint<…>] / [ShiftEntitySecureEndpoint<…>] (e.g. Country) and registers their built-in
// repository + DTO-map entry; the mapping itself comes from the source-generated mapper unless the attribute
// names one. Map the routes below with app.MapShiftEntityEndpoints<DB>().
builder.Services.RegisterShiftRepositories(typeof(StockPlusPlus.Data.Marker).Assembly);

builder.Services.AddAttentionEvaluator<IHasDueDate, FrameworkOverdueEvaluator>();
#if (includeSampleApp)
builder.Services.AddAttentionEvaluator<StockPlusPlus.Data.Entities.Invoice, InvoiceMissingReferenceEvaluator>();
// Composition: a second evaluator on Product raises a "Compliance"-scoped signal alongside
// Product's own (default-scope) "ReleasedWithoutPrice" — demonstrates scoped clearing.
builder.Services.AddAttentionEvaluator<StockPlusPlus.Data.Entities.Product, StockPlusPlus.Data.Evaluators.ProductComplianceEvaluator>();
#endif

#if (includeSampleApp)
// The standalone attention endpoints (app.MapAttentionEndpoints<DB>() below) decide access per
// entity type through the ShiftEntityActionMap registry: the same TypeAuth action that secures
// the entity's own endpoints. Signals of a type the registry does not know are denied by
// default, so they are left out of GET api/attention/active. Three surfaces feed the registry:
//   - Attribute endpoints ([ShiftEntitySecureEndpoint<…>], e.g. Country) feed it automatically
//     through RegisterShiftRepositories above.
//   - Minimal API (MapShiftEntitySecureCrud, e.g. Product in MapProductMinimalApi below) feeds
//     it automatically at map time.
//   - Classic controllers (ShiftEntitySecureControllerAsync, e.g. InvoiceController) need this
//     explicit call. The controller receives its action through its constructor, so the
//     framework cannot see the action at startup.
builder.Services.AddShiftEntityAction<StockPlusPlus.Data.Entities.Invoice>(StockPlusPlusActionTree.Invoice);
#endif

#if (includeSampleApp)
// Generic overload also registers StockPlusPlusActionTree with TypeAuth (idempotent — the explicit
// AddActionTree in AddTypeAuth below stays for the other entities; no duplicate).
builder.Services.AddShiftTagging<DB, StockPlusPlusActionTree>(StockPlusPlusActionTree.Tags);
#endif

// Phase 2 emission: once a save that raised attention signals commits, the framework
// publishes one AttentionRaised event per new signal to the registered consumers (on a
// background drain loop — consumer latency never affects the save). This sample consumer
// just logs each event; a real one would send email / push / audit instead.
builder.Services.AddAttentionConsumer<StockPlusPlus.API.Services.AttentionLoggingConsumer>();

// Phase 2 real-time (Iteration 8, server side): registers SignalR + AttentionRealtimeNotifier,
// which fans each raised signal to the AttentionHub group for its entity type. The matching
// app.MapAttentionHub() below exposes the hub endpoint. The ShiftList / ShiftEntityForm client
// switches that subscribe and react land in Iteration 9.
builder.Services.AddAttentionHub();

// Mapping: every repository's four maps (entity <-> view, entity -> list, entity -> entity) are declared by
// ShiftMapper from the repository's type arguments and the endpoint attributes, in the Data project's build,
// with the framework's conventions — nothing is registered here; RegisterShiftRepositories above registers
// the Data assembly's generated mapper. The sample shows every door:
//   - Country, ProductCategory -> AUTOMATIC: nothing written (CountryRepository, ProductCategoryRepository,
//                                the api/country endpoint)
//   - Invoice, api/country-generated -> automatic maps CUSTOMIZED where the repository is configured:
//                                options.Mapping(m => m.List.ForMember(...)) in InvoiceRepository, and the
//                                same from the entity's ConfigureRepository for api/country-generated
//   - ProductBrand    -> a MAPPER CLASS (Mappers/ProductBrandMapper.cs, an ordinary ShiftMapperBase) whose
//                        CreateMaps replace the automatic maps for the pairs it declares
//   - Product         -> overrides MapToView/MapToEntity/MapToList in ProductRepository (unchanged door)
//   - api/countrymapped -> a hand-written IShiftEntityMapper (Mappers/CountryMapper.cs) (unchanged door)
// The same maps serve any service that injects Mapper (typed methods) or IMapper. A triple nothing covers
// throws at startup (ShiftEntityMapperValidation) rather than mapping by convention at request time.

builder.Services.AddDbContext<DB>(dbOptionBuilder);
builder.Services.AddHttpClient();

#if (includeSampleApp)
// One explicit host opt-in installs the v2 engine and its seven standard marker dimensions. The Vehicle repository
// replaces the standard single-key Companies dimension with a two-key owner-or-intermediary rule.
builder.Services.AddShiftEntityDataLevelAccess();
#endif

//builder.Services.AddScoped<IFileExplorerAccessControl, FileManagerAccessControl>();

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
        string databaseId = IdentityDatabaseAndContainerNames.DatabaseName;
        var client = x.Services.GetRequiredService<CosmosClient>();

        // Reusable replication setup. Each SetUpXReplication (+ this aggregate) lives in
        // ShiftIdentity.Dashboard.AspNetCore/Replication and passes no mapping delegates: the documents are mapped
        // through the ShiftMapper mapper AddShiftIdentityDashboard<DB>() (below) registers. Call individual
        // SetUpXReplication<DB>(...) methods to customize.
        x.SetUpAllIdentityReplications<DB>(client, databaseId);
    });

    builder.Services.AddShiftEntityCosmosDbReplication<DB>();
#endif
}

var mvcBuilder = builder.Services
    .AddLocalization()
    .AddControllers();

builder.Services.AddShiftEntityPrint(x =>
{
    x.TokenExpirationInSeconds = 600;
    x.SASTokenKey = "One-Two-Three";
});

mvcBuilder.AddShiftEntityWeb(x =>
{
    x.AddDataAssembly(typeof(StockPlusPlus.Data.Marker).Assembly);
    x.WrapValidationErrorResponseWithShiftEntityResponse(true);

    x.HashId.RegisterHashId(builder.Configuration.GetValue<bool>("Settings:HashIdSettings:AcceptUnencodedIds"));
    x.HashId.RegisterIdentityHashId("one-two", 5);

    var azureStorageAccounts = new List<ShiftSoftware.ShiftEntity.Core.Services.AzureStorageOption>();

    builder.Configuration.Bind("AzureStorageAccounts", azureStorageAccounts);

    x.AddAzureStorage(azureStorageAccounts.ToArray());
#if (internalShiftIdentityHosting)
    // Lets app.MapShiftEntityEndpoints<DB>() (below) discover ShiftIdentity's own attribute-driven CRUD
    // endpoints — the entities carrying [ShiftEntitySecureEndpoint<…>] (Brand, Service, Department).
    // Their DI half is wired inside AddShiftIdentityDashboard.
    x.AddShiftIdentityDataAssembly();
#endif
});

mvcBuilder.AddShiftIdentity(builder.Configuration.GetValue<string>("Settings:TokenSettings:Issuer")!,
    builder.Configuration.GetValue<string>("Settings:TokenSettings:PublicKey")!);

#if (internalShiftIdentityHosting)

// Configure SecurityEmail:Smtp locally (credentials in user secrets). Missing settings fail delivery;
// no message is reported accepted until the SMTP server accepts it.
builder.Services.Configure<SecurityEmailSmtpOptions>(builder.Configuration.GetSection("SecurityEmail:Smtp"));
builder.Services.AddScoped<SmtpSecurityEmailSender>();
builder.Services.AddScoped<ISecurityEmailSender, SendEmailService>();
builder.Services.AddScoped<ISendEmailVerification, SendEmailService>();
builder.Services.AddScoped<ISendEmailResetPassword, SendEmailService>();

mvcBuilder.AddShiftIdentityDashboard<DB>(
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
            RequirePasswordChange = true,
        },
        MfaSettings = new MfaSettingsModel
        {
            Enabled = builder.Configuration.GetValue<bool>("Settings:Mfa:Enabled", true),
            Mandatory = builder.Configuration.GetValue<bool>("Settings:Mfa:Mandatory", false),
            Totp = new TotpSettingsModel { IssuerName = "identity.shift.software" }
        },
        // The identity authority (Settings:Authority): SQL-admitted logins, versioned sessions, protected
        // authenticators and the api/identity/v2 account flows, with the deployed routes keeping their shapes.
        // Settings:FactorProtection holds the keys that protect stored authenticators. The Web client's
        // ShiftIdentityAuthority setting must match Authority:Enabled. Requires the identity security tables
        // (the AddIdentitySecurity migration); a host without them stops at startup and says so.
        Authority = builder.Configuration.GetSection("Settings:Authority").Get<AuthoritySettingsModel>() ?? new(),
        FactorProtection = builder.Configuration.GetSection("Settings:FactorProtection").Get<FactorProtectionSettings>() ?? new(),
        FrontEndUrl = builder.Configuration.GetValue<string>("Settings:FrontEndUrl"),
        EmailVerificationRedirectUrl = builder.Configuration.GetValue<string>("Settings:EmailVerificationRedirectUrl"),
        TemporaryTokenSettings = new TemporaryTokenSettingsModel
        {
            Key = builder.Configuration.GetValue<string>("Settings:TokenSettings:TemporaryTokenKey")!,
            Issuer = builder.Configuration.GetValue<string>("Settings:TokenSettings:Issuer")!,
            Audience = "stock-plus-plus",
            ExpireSeconds = 100000,
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
//mvcBuilder.AddShiftEntityOdata(x =>
//{
//    x.DefaultOptions();
//    x.RegisterAllDTOs(typeof(StockPlusPlus.Shared.Marker).Assembly);
//#if (includeSampleApp)
//    x.OdataEntitySet<ServiceListDTO>("Service");
//#endif
//#if (internalShiftIdentityHosting)
//    x.RegisterShiftIdentityDashboardEntitySets();
//#endif
//});

#if (externalShiftIdentityHosting)
if (builder.Environment.IsDevelopment())
{
    mvcBuilder.AddFakeIdentityEndPoints(
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
            RedirectUri = "http://localhost:5069/Auth/Token"
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

//builder.Services.AddSwaggerGen(c =>
//{

//});

builder.Services.AddTypeAuth((o) =>
{
    o.AddActionTree<ShiftIdentityActions>();
    o.AddActionTree<ShiftSoftware.ShiftEntity.Core.GeneralActionTree>();
    o.AddActionTree<ShiftSoftware.ShiftEntity.Core.AzureStorageActionTree>();
#if (includeSampleApp)
    o.AddActionTree<StockPlusPlus.Shared.ActionTrees.StockPlusPlusActionTree>();
#endif
});

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddRazorPages();
}
builder.Services.AddAzureClients(clientBuilder =>
{
    clientBuilder.AddBlobServiceClient(builder.Configuration["devstoreaccount1:blob"]!, preferMsi: true);
    clientBuilder.AddQueueServiceClient(builder.Configuration["devstoreaccount1:queue"]!, preferMsi: true);
});

var app = builder.Build();

#if (externalShiftIdentityHosting)
if (builder.Environment.IsDevelopment())
{
    app.AddFakeIdentityEndPoints();
}
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

var supportedCultures = new List<CultureInfo>
{
    new("en-US"),
    new("ar-IQ"),
    new("ku-IQ"),
    new("fr-FR"),
};

app.UseRequestLocalization(options =>
{
    options.SetDefaultCulture("en-US");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
    options.RequestCultureProviders = [new AcceptLanguageHeaderRequestCultureProvider()];
    options.ApplyCurrentCultureToResponseHeaders = true;
});

app.MapControllers();
app.MapAttentionEndpoints<DB>();
app.MapAttentionHub();

// Maps the attribute-driven CRUD endpoints (entities decorated with [ShiftEntityEndpoint<…>] /
// [ShiftEntitySecureEndpoint<…>], e.g. Country). DI was wired by RegisterShiftRepositories above; with
// no assemblies passed it discovers them from the registered data assemblies.
app.MapShiftEntityEndpoints<DB>();

#if (internalShiftIdentityHosting)
// ShiftIdentity endpoints are hosted ONLY when this app IS the identity server (internal hosting). Under EXTERNAL
// hosting the app is a microservice that CALLS a separate identity, so it must NOT host login/token/dashboard — it
// only validates tokens (AddShiftIdentity above, which stays unconditional).
//
// One entry point for the whole identity server: MapShiftIdentityDashboard() maps auth
// (login/refresh/MFA/auth-code/external-token, from ShiftIdentity.AspNetCore) AND the dashboard's custom endpoints
// (CompanyCalendar's GetCalendarEvents, User, …). Auth is part of the identity server, not a separate concern.
app.MapShiftIdentityDashboard();
#endif

#if (includeSampleApp)
app.MapShiftTaggingEndpoints<DB>();
#endif

#if (includeSampleApp)
// Minimal-API surface running side-by-side with the controllers, driven by the same
// ShiftEntityCrudHandler — proves the refactor is lossless and demonstrates the
// MapShiftEntitySecureCrud / RequireTypeAuth* extensions end-to-end.
app.MapProductMinimalApi();
#endif

app.UseCors(x => x.WithOrigins("*").AllowAnyMethod().AllowAnyHeader());

if (app.Environment.IsDevelopment())
{
    app.UseBlazorFrameworkFiles();
    app.UseStaticFiles();

    //app.UseSwagger();
    //app.UseSwaggerUI();

    app.MapRazorPages();
    app.MapFallbackToFile("index.html");
}

app.Run();
#endif
