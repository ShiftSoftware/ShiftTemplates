#if shiftFrameworkDevelopmentMode
using System.Text.RegularExpressions;
using EntityFrameworkCore.Triggered;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StockPlusPlus.Data.Entities;

namespace StockPlusPlus.Test.Tests;

/// <summary>
/// The framework's save handlers have taken the place of EntityFrameworkCore.Triggered, and the framework no longer
/// switches that library on. A host that still registers one of its triggers would lose it without a sign, so the host
/// does not start, and the message says how to convert the trigger. This is the only test that references the library.
/// </summary>
public sealed class TriggerLibraryCheckTests : IAsyncLifetime
{
    private const string Server = @"Server=.\SQLEXPRESS;Database=master;Integrated Security=true;Encrypt=true;TrustServerCertificate=true;Connect Timeout=5";
    private readonly string database = "StockPlusPlus_Test_Triggers_" + Guid.NewGuid().ToString("N");
    private TriggerFactory? factory;
    private bool owned;

    public async ValueTask InitializeAsync()
    {
        await using var sql = new SqlConnection(Server); await sql.OpenAsync(TestContext.Current.CancellationToken);
        await using var check = sql.CreateCommand(); check.CommandText = "SELECT DB_ID(@name)"; check.Parameters.AddWithValue("@name", database);
        Assert.Equal(DBNull.Value, await check.ExecuteScalarAsync(TestContext.Current.CancellationToken));
        owned = true;
        factory = new(new SqlConnectionStringBuilder(Server) { InitialCatalog = database }.ConnectionString);
    }

    [Fact]
    public void A_host_with_a_trigger_registered_does_not_start_and_says_how_to_convert_it()
    {
        var refused = Assert.ThrowsAny<Exception>(() => factory!.CreateClient());

        // The host's own exception, or one that carries it.
        var message = refused.ToString();

        Assert.Contains("This host still uses EntityFrameworkCore.Triggered", message);
        Assert.Contains("Registered in its services: ProductPriceTrigger as IAfterSaveTrigger<Product>.", message);
        Assert.Contains("An after-save trigger (IAfterSaveTrigger<T>) becomes an IAfterCommitHandler<T> when it tells another system", message);
        Assert.Contains("or an IAfterSaveHandler<T> when it writes something that belongs to the save", message);
        Assert.Contains("Then remove UseTriggers and the package reference.", message);
    }

    public async ValueTask DisposeAsync()
    {
        try { if (factory is not null) await factory.DisposeAsync(); }
        finally
        {
            if (owned)
            {
                if (!Regex.IsMatch(database, @"^StockPlusPlus_Test_Triggers_[a-f0-9]{32}$")) throw new InvalidOperationException("Invalid fixture database name.");
                SqlConnection.ClearAllPools();
                await using var sql = new SqlConnection(Server); await sql.OpenAsync();
                await using var drop = sql.CreateCommand();
                drop.CommandText = $"IF DB_ID(@name) IS NOT NULL BEGIN ALTER DATABASE [{database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{database}]; END; SELECT DB_ID(@name);";
                drop.Parameters.AddWithValue("@name", database);
                Assert.Equal(DBNull.Value, await drop.ExecuteScalarAsync());
            }
        }
    }

    /// <summary>The sample's host, as it is, with one trigger of the old library left in its services.</summary>
    private sealed class TriggerFactory(string connection) : CustomWebApplicationFactory
    {
        protected override IConfigurationBuilder CreateTestConfigurationBuilder() => base.CreateTestConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { ["ConnectionStrings:SQLServer_Test"] = connection });

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureServices(services => services.AddScoped<IAfterSaveTrigger<Product>, ProductPriceTrigger>());
        }
    }

    private sealed class ProductPriceTrigger : IAfterSaveTrigger<Product>
    {
        public Task AfterSave(ITriggerContext<Product> context, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
#endif
