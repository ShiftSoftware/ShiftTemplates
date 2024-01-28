using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StockPlusPlus.Data.Repositories.Product;
using StockPlusPlus.Data;
using System.Configuration;
using Microsoft.Extensions.Configuration;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureAppConfiguration(builder =>
    {
        builder.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
        .AddEnvironmentVariables().AddUserSecrets<Program>(optional: true, reloadOnChange: true);
        var config = builder.Build();
    })
    .ConfigureServices((hostBuilder, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        
        services
        .AddShiftEntity(x =>
        {
            x.HashId.RegisterHashId(false);

            x.AddAutoMapper(typeof(ShiftSoftware.ShiftEntity.EFCore.AutoMapperProfiles.DefaultMappings).Assembly);
            x.AddAutoMapper(typeof(StockPlusPlus.Data.Marker).Assembly);
            x.AddAutoMapper(typeof(ShiftSoftware.ShiftIdentity.Data.Marker).Assembly);
        })
            .RegisterShiftEntityEfCoreTriggers()
            .AddDbContext<DB>(options => options.UseSqlServer(hostBuilder.Configuration.GetConnectionString("SQLServer")))
            .AddScoped<ProductCategoryRepository>()
            .AddScoped<BrandRepository>()
            .AddScoped<ProductRepository>();

        services.AddShiftEntityCosmosDbReplication();
    })
    .Build();

host.Run();