using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ShiftIdentity.Data;
using Test.Data.Entities;

namespace ShiftTemplates.Builder.Migrator;

// Owns the schema for TestNET10Auto, the shared SQL DB used by every Builder-generated
// test project. The Builder invokes `dotnet ef` against this project on each run, so this
// is the single place migrations are scaffolded and applied.
//
// Internal hosting is the superset (ShiftIdentityDbContext + sample-app DbSets). External
// test projects use ShiftDbContext (subset of ShiftIdentityDbContext) plus the same sample-app
// DbSets, so running them against TestNET10Auto works without conflict.
//
// DbSet property names mirror Test.Data.DbContext.DB exactly — EF uses the property name as
// the default table name, so any mismatch would create a parallel set of tables the test
// projects can't see.
public class MigratorDbContext : ShiftIdentityDbContext
{
    public MigratorDbContext(DbContextOptions<MigratorDbContext> options) : base(options)
    {
    }

#pragma warning disable CS0108 // Member hides inherited member; missing new keyword
    public DbSet<ProductBrand> Brands { get; set; } = default!;
    public DbSet<ProductCategory> ProductCategories { get; set; } = default!;
    public DbSet<Product> Products { get; set; } = default!;
    public DbSet<Country> Countries { get; set; } = default!;
    public DbSet<Invoice> Invoices { get; set; } = default!;
    public DbSet<InvoiceLine> InvoiceLines { get; set; } = default!;
#pragma warning restore CS0108 // Member hides inherited member; missing new keyword
}
