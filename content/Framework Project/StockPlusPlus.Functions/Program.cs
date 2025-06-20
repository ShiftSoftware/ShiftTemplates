using FluentValidation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Functions.Extensions;
using ShiftSoftware.ShiftIdentity.AspNetCore.Extensions;
using ShiftSoftware.ShiftIdentity.Core;
using ShiftSoftware.TypeAuth.AspNetCore.Extensions;
using StockPlusPlus.Data.DbContext;
using StockPlusPlus.Functions;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Abstractions;
using ShiftSoftware.ShiftEntity.Functions.Services;
#if (includeSampleApp)
using StockPlusPlus.Data.Repositories;

#endif

#if (includeSampleApp)
#endif
using System.Security.Claims;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication((h, x) =>
    {
        var issuer = "Please-Change-This-Issuer";
        var key = "MIIBCgKCAQEAl4cuKYvpxSW723/Evi/fBvIQ97CjV56jivFx2sMRUs+qIZoFqGtcsg9xkcS0PjCPNyH9Db3oK1jSYNaWwGXvoP+ruVPJAKMRlvGyxPcD7roSOKQAbbzpsfkDBCIMexLlZ9r2DAcuz3CntFkQvf5rL5hpM905aHJmObNcRNzeKl12X/o9jEJcoxX8YZ3nEllGLRwCfox6f/a7aXeiT7HaHEzM1vj2VmDmUSq/HlGrd1+Q8uS6RWUWtCIYmifXQVIP3yr/3uCvw4nXC3CQesstzcmmDoiRPU88+ChG16RoA4UCFbqOG7Ce1q5gyfRR5mgu4s0TQUj4Q1KNfpKRU3BT3QIDAQAB";

        x.AddShiftIdentity(issuer, key);
        x.AddGoogleReCaptcha("");

        x.AddFirebaseAppCheck(
           h.Configuration.GetValue<string>("FirebaseAppCheck:ProjectNumber")!,
           h.Configuration.GetValue<string>("FirebaseAppCheck:ServiceAccount")!,
           h.Configuration.GetValue<string>("HMS:ClientID")!,
           h.Configuration.GetValue<string>("HMS:ClientSecret")!,
           h.Configuration.GetValue<string>("HMS:AppId")!);

        x.RequireValidModels(true);

        x.UseRequestLocalization();
    })
    .ConfigureAppConfiguration(builder =>
    {
        builder.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
        .AddEnvironmentVariables().AddUserSecrets<Program>(optional: true, reloadOnChange: true);
        var config = builder.Build();
    })
    .ConfigureServices((hostBuilder, services) =>
    {
        services.AddValidatorsFromAssemblyContaining<Program>();

        services.AddSingleton<OpenApiConfigurationOptions>();

        var openApiKey = hostBuilder.Configuration["OpenApi:ApiKey"]!;
        
        services.AddSingleton<IOpenApiHttpTriggerAuthorization, OpenApiHttpTriggerAuthorization>(x => new OpenApiHttpTriggerAuthorization(openApiKey));

        services.RegisterShiftRepositories(typeof(StockPlusPlus.Data.Marker).Assembly);

        services.AddLocalization();

        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services
        .AddShiftEntity(x =>
        {
            x.AddDataAssembly(typeof(StockPlusPlus.Data.Marker).Assembly);
            x.HashId.RegisterHashId(false);

            var azureStorageAccounts = new List<ShiftSoftware.ShiftEntity.Core.Services.AzureStorageOption>();

            hostBuilder.Configuration.Bind("AzureStorageAccounts", azureStorageAccounts);

            x.AddAzureStorage(azureStorageAccounts.ToArray());
        })
        .RegisterShiftEntityEfCoreTriggers()
        .AddDbContext<DB>(options => options.UseSqlServer(hostBuilder.Configuration.GetConnectionString("SQLServer")!));

#if (includeSampleApp)
        //services.AddScoped<ProductCategoryRepository>()
        //.AddScoped<ProductBrandRepository>()
        //.AddScoped<ProductRepository>();
#endif

        services.AddShiftEntityCosmosDbReplication();

        services.AddTypeAuth((o) =>
        {
            o.AddActionTree<ShiftIdentityActions>();
            o.AddActionTree<AzureStorageActionTree>();
#if (includeSampleApp)
            o.AddActionTree<StockPlusPlus.Shared.ActionTrees.StockPlusPlusActionTree>();
#endif
        });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("Test", policy => policy.RequireClaim(ClaimTypes.Name, "SuperUser2"));
            options.AddPolicy("Test2", policy => policy.RequireClaim(ClaimTypes.Name, "SuperUser"));
        });

        services.AddHttpClient();
    })
    .Build();

host.Run();