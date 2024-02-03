using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using ShiftSoftware.Azure.Functions.AspNetCore.Authorization.Extensions;
using StockPlusPlus.Data;
using StockPlusPlus.Data.Repositories.Product;
using System.Text;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults(x =>
    {
        x.AddAuthentication().AddJwtBearer(
            new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = true,
                ValidIssuer = "StockPlusPlus",
                RequireExpirationTime = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("one-two-three-four-five-six-seven-eight.one-two-three-four-five-six-seven-eight")),
                ValidateIssuerSigningKey = true,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
                LifetimeValidator = (DateTime? notBefore, DateTime? expires, SecurityToken securityToken,
                                     TokenValidationParameters validationParameters) =>
                {
                    bool result = false;
                    var now = DateTime.UtcNow;

                    if (notBefore != null && now < notBefore)
                        result = false;

                    if (expires != null)
                        result = expires > now;

                    if (!result)
                        throw new SecurityTokenExpiredException("Token expired");

                    return result;
                }
            }
        );
    })
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