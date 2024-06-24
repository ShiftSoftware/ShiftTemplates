using FluentValidation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ShiftSoftware.ShiftEntity.Functions.Extensions;
using ShiftSoftware.ShiftIdentity.AspNetCore.Extensions;
using ShiftSoftware.ShiftIdentity.Core;
using ShiftSoftware.TypeAuth.AspNetCore.Extensions;
using StockPlusPlus.Data.DbContext;
#if (includeSampleApp)
using StockPlusPlus.Data.Repositories;
#endif

#if (includeSampleApp)
#endif
using System.Security.Claims;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication((h, x) =>
    {
        var issuer= "Please-Change-This-Issuer";
        var key = "Please-Change-This-Key:one-two-three-four-five-six-seven-eight.one-two-three-four-five-six-seven-eight";

        x.AddShiftIdentity(issuer, key);
        x.AddGoogleReCaptcha("");

        x.AddFirebaseAppCheck(
           h.Configuration.GetValue<string>("FirebaseAppCheck:ProjectNumber")!,
           h.Configuration.GetValue<string>("FirebaseAppCheck:ServiceAccount")!,
           h.Configuration.GetValue<string>("HMS:ClientID")!,
           h.Configuration.GetValue<string>("HMS:ClientSecret")!,
           h.Configuration.GetValue<string>("HMS:AppId")!);

        x.RequireValidModels(true);
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

        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services
        .AddShiftEntity(x =>
        {
            x.HashId.RegisterHashId(false);
        })
            .RegisterShiftEntityEfCoreTriggers()
            .AddDbContext<DB>(options => options.UseSqlServer(hostBuilder.Configuration.GetConnectionString("SQLServer")!));

#if (includeSampleApp)
        services.AddScoped<ProductCategoryRepository>()
        .AddScoped<ProductBrandRepository>()
        .AddScoped<ProductRepository>();
#endif

        services.AddShiftEntityCosmosDbReplication();

        services.AddTypeAuth((o) =>
        {
            o.AddActionTree<ShiftIdentityActions>();
#if (includeSampleApp)
            o.AddActionTree<StockPlusPlus.Shared.ActionTrees.SystemActionTrees>();
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