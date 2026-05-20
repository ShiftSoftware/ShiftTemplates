using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using ShiftSoftware.ShiftEntity.EFCore;

namespace ShiftTemplates.Builder.Migrator;

// Lets `dotnet ef` (which doesn't know about Host/DI) construct the context at design time.
// Reads the connection string from appsettings.json so there's one source of truth for the
// DB the test projects also point at.
public class MigratorDbContextFactory : IDesignTimeDbContextFactory<MigratorDbContext>
{
    public MigratorDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var connectionString = configuration.GetConnectionString("SQLServer")
            ?? throw new InvalidOperationException("ConnectionStrings:SQLServer is not configured.");

        // UseTemporal must match the runtime DbContextOptions wiring in Test.Server's Program.cs
        // (which calls .UseTemporal(true) when building the DB options). If the migration is
        // scaffolded without it, the generated schema lacks the temporal PeriodStart/PeriodEnd
        // columns and any query at runtime fails with "Invalid column name 'PeriodEnd'".
        var optionsBuilder = new DbContextOptionsBuilder<MigratorDbContext>();
        optionsBuilder.UseSqlServer(connectionString).UseTemporal(true);

        return new MigratorDbContext(optionsBuilder.Options);
    }
}
