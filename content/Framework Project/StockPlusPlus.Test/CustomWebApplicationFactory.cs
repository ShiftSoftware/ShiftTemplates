using ShiftSoftware.ShiftFrameworkTestingTools;
using ShiftSoftware.ShiftIdentity.Core;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StockPlusPlus.API;
using StockPlusPlus.Data.DbContext;

#if (includeSampleApp)
using StockPlusPlus.Shared.ActionTrees;
#endif

namespace StockPlusPlus.Test;

public class CustomWebApplicationFactory : ShiftCustomWebApplicationFactory<WebMarker, DB>
{
    // Completed once this factory has created the test database (see CreateHost).
    private readonly TaskCompletionSource databaseReady = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public CustomWebApplicationFactory() : base(
        "SQLServer_Test",
        new ShiftCustomWebApplicationBearerAuthSettings
        {
            Enabled = true,
            // The API validates RSA-signed tokens (AddShiftIdentity), so the test token must be
            // signed with the private key — the public key would silently produce 401s.
            TokenKeySettingKey = "Settings:TokenSettings:PrivateKey",
            TokenIssuerSettingKey = "Settings:TokenSettings:Issuer",
            TypeAuthActions = new List<Type>()
            {
                // Framework tree: DataGridExport permits list GETs without a $top restriction.
                typeof(ShiftSoftware.ShiftEntity.Core.GeneralActionTree),
#if (includeSampleApp)
                typeof(StockPlusPlusActionTree),
                typeof(ShiftIdentityActions)
#endif
            }
        })
    { }

    // Verification runs must explicitly select a test override, such as SHIFT_TEST_ConnectionStrings__SQLServer_Test.
    protected override IConfigurationBuilder CreateTestConfigurationBuilder()
        => base.CreateTestConfigurationBuilder().AddEnvironmentVariables("SHIFT_TEST_");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        // WebApplicationFactory runs Program's app.Run() as soon as the host is built, concurrently with CreateHost,
        // and the identity authority readies its policy and client rows when the host starts. Its hosted services
        // therefore have to wait for the database this factory creates: this gate is registered before every other
        // hosted service (they start in registration order) and holds them until CreateHost has created the database.
        builder.ConfigureServices(services =>
            services.Insert(0, ServiceDescriptor.Singleton<IHostedService>(new DatabaseReadyGate(databaseReady.Task))));
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.UseEnvironment("Test");
        var host = builder.Build();
        try
        {
            using var scope = host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DB>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();
        }
        finally
        {
            // Released even after a failure, so the application fails at startup instead of waiting forever.
            databaseReady.TrySetResult();
        }
        host.Start();
        return host;
    }

    private sealed class DatabaseReadyGate(Task ready) : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken) => ready.WaitAsync(cancellationToken);
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
