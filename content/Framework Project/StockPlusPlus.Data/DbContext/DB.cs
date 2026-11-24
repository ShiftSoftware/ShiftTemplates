
using Microsoft.EntityFrameworkCore;
#if (internalShiftIdentityHosting)
using ShiftSoftware.ShiftIdentity.Data;
#elif (externalShiftIdentityHosting)
using ShiftSoftware.ShiftEntity.EFCore;
#endif

#if (includeSampleApp)
using StockPlusPlus.Data.Entities;
using StockPlusPlus.Data.SaveHandlers;
#endif

namespace StockPlusPlus.Data.DbContext;

#if (internalShiftIdentityHosting)
public partial class DB : ShiftIdentityDbContext
#elif (externalShiftIdentityHosting)
public partial class DB : ShiftDbContext
#endif
{
    public DB(DbContextOptions<DB> option) : base(option)
    {
    }

#if (includeSampleApp)
#pragma warning disable CS0108 // Member hides inherited member; missing new keyword
    public DbSet<ProductBrand> Brands { get; set; }
    public DbSet<ProductCategory> ProductCategories { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Country> Countries { get; set; }
    public DbSet<SampleContact> SampleContacts { get; set; }
    public DbSet<SampleContactPhone> SampleContactPhones { get; set; }
    public DbSet<Invoice> Invoices { get; set; }
    public DbSet<InvoiceLine> InvoiceLines { get; set; }
    public DbSet<Vehicle> Vehicles { get; set; }
#pragma warning restore CS0108 // Member hides inherited member; missing new keyword

    // The save handlers that belong to the data. Declared here, they run for every save of this context, in any host:
    // the API, the Functions app, a seeder, a test. A handler that needs a service of one host is declared by that
    // host instead (AddSaveHandler in Program.cs).
    protected override void ConfigureSaveHandlers(ShiftSoftware.ShiftEntity.EFCore.SaveHandlerSet handlers)
    {
        base.ConfigureSaveHandlers(handlers);

        handlers
            .Add<InvoiceNumbering>()             // before save: the next invoice number, under a lock
            .Add<ProductCategoryCode>()          // after save: a code made from the new ID, in the same transaction
            .Add<NoDraftProductsOnInvoices>();   // before commit: a rule over everything the transaction wrote
    }
#endif
}
