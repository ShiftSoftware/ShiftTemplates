using Microsoft.EntityFrameworkCore;
using StockPlusPlus.Data.DbContext;
using StockPlusPlus.Data.Entities;
using StockPlusPlus.Shared.Enums;

namespace StockPlusPlus.API;

/// <summary>
/// Dev-only seed that tops up Invoices and Products so UI prototypes (like the
/// attention/notifications work) have enough realistic rows to demo against.
/// Idempotent — only inserts if counts are below a threshold. Runs in Development only.
/// </summary>
internal static class DevSampleSeed
{
    private const int InvoiceTarget = 15;
    private const int ProductTarget = 15;

    public static async Task SeedAsync(WebApplication app)
    {
        if (!app.Environment.IsDevelopment()) return;

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DB>();

        var brandId = await EnsureBrandAsync(db);
        var categoryId = await EnsureCategoryAsync(db);
        await TopUpProductsAsync(db, brandId, categoryId);
        await TopUpInvoicesAsync(db);
    }

    private static async Task<long> EnsureBrandAsync(DB db)
    {
        var existing = await db.Brands.Select(x => x.ID).FirstOrDefaultAsync();
        if (existing != 0) return existing;

        var brand = new ProductBrand
        {
            Name = "Acme",
            Code = "ACME",
            CreateDate = DateTimeOffset.Now,
            LastSaveDate = DateTimeOffset.Now,
            AuditFieldsAreSet = true,
        };
        db.Brands.Add(brand);
        await db.SaveChangesAsync();
        return brand.ID;
    }

    private static async Task<long> EnsureCategoryAsync(DB db)
    {
        var existing = await db.ProductCategories.Select(x => x.ID).FirstOrDefaultAsync();
        if (existing != 0) return existing;

        var category = new ProductCategory
        {
            Name = "General",
            Code = "GEN",
            TrackingMethod = TrackingMethod.Serial,
            CreateDate = DateTimeOffset.Now,
            LastSaveDate = DateTimeOffset.Now,
            AuditFieldsAreSet = true,
        };
        db.ProductCategories.Add(category);
        await db.SaveChangesAsync();
        return category.ID;
    }

    private static async Task TopUpProductsAsync(DB db, long brandId, long categoryId)
    {
        var current = await db.Products.CountAsync();
        if (current >= ProductTarget) return;

        var names = new[]
        {
            "Cordless Drill 18V", "Laser Printer Toner", "HDMI Cable 2m",
            "Ergonomic Keyboard", "USB-C Hub", "Wireless Mouse",
            "LED Desk Lamp", "Noise-Cancelling Headphones", "External SSD 1TB",
            "Mechanical Keyboard", "Webcam 1080p", "Portable Monitor 15\"",
            "Office Chair", "Standing Desk Converter", "Document Shredder",
        };

        var rnd = new Random(42);
        var toAdd = ProductTarget - current;
        for (var i = 0; i < toAdd; i++)
        {
            var name = names[i % names.Length] + (i >= names.Length ? $" Mk{i / names.Length + 1}" : "");
            db.Products.Add(new Product
            {
                Name = name,
                TrackingMethod = (TrackingMethod)(i % 3),
                Price = 10 + rnd.Next(0, 50) * 5,
                ProductBrandID = brandId,
                ProductCategoryID = categoryId,
                ReleaseDate = DateTimeOffset.Now.AddDays(-rnd.Next(1, 400)),
                IsDraft = false,
                CreateDate = DateTimeOffset.Now,
                LastSaveDate = DateTimeOffset.Now,
                AuditFieldsAreSet = true,
            });
        }

        await db.SaveChangesAsync();
    }

    private static async Task TopUpInvoicesAsync(DB db)
    {
        var current = await db.Invoices.CountAsync();
        if (current >= InvoiceTarget) return;

        var maxNo = await db.Invoices.AnyAsync()
            ? await db.Invoices.MaxAsync(x => x.InvoiceNo)
            : 1000L;

        var productIds = await db.Products.Select(p => p.ID).Take(10).ToListAsync();
        if (productIds.Count == 0) return;

        var rnd = new Random(7);
        var toAdd = InvoiceTarget - current;
        for (var i = 0; i < toAdd; i++)
        {
            maxNo++;
            var invoice = new Invoice
            {
                ManualReference = $"REF-{1000 + i:D4}",
                InvoiceNo = maxNo,
                InvoiceDate = DateTimeOffset.Now.AddDays(-rnd.Next(0, 60)),
                CreateDate = DateTimeOffset.Now,
                LastSaveDate = DateTimeOffset.Now,
                AuditFieldsAreSet = true,
            };

            var lineCount = 1 + rnd.Next(0, 3);
            for (var l = 0; l < lineCount; l++)
            {
                var productId = productIds[rnd.Next(productIds.Count)];
                invoice.InvoiceLines.Add(new InvoiceLine
                {
                    Description = $"Line item {l + 1}",
                    Price = 10m + rnd.Next(1, 100) * 2.5m,
                    ProductID = productId,
                    CreateDate = DateTimeOffset.Now,
                    LastSaveDate = DateTimeOffset.Now,
                    AuditFieldsAreSet = true,
                });
            }

            db.Invoices.Add(invoice);
        }

        await db.SaveChangesAsync();
    }
}
