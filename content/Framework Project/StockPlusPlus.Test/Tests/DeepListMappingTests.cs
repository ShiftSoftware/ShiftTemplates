using System.Linq;
using ShiftSoftware.ShiftEntity.Core;
using StockPlusPlus.Data.Entities;
using StockPlusPlus.Shared.DTOs.Invoice;

namespace StockPlusPlus.Test.Tests;

/// <summary>
/// DEEP LIST mapping via the config-driven opt-in. No [ShiftEntityMapper] partial and no attribute is
/// declared for (InvoiceLine, InvoiceLineListDTO): the <c>ForListChildren</c> CALL in InvoiceRepository is
/// itself the opt-in that makes the source generator emit that pair — including a list projection that
/// carries the line's Product as a ShiftEntitySelectDTO (FK id + navigation name), SQL-translatable.
/// Here we apply the identical call and run MapToList in-memory: list → lines → product.
/// </summary>
public class DeepListMappingTests
{
    private static IShiftEntityMapper<Invoice, InvoiceListDTO, InvoiceDTO> ResolveInvoiceMapper()
    {
        System.Runtime.CompilerServices.RuntimeHelpers.RunModuleConstructor(typeof(Invoice).Module.ModuleHandle);

        var mapperType = ShiftEntityMapperRegistry.Find(typeof(Invoice), typeof(InvoiceListDTO), typeof(InvoiceDTO));
        Assert.NotNull(mapperType);

        return (IShiftEntityMapper<Invoice, InvoiceListDTO, InvoiceDTO>)Activator.CreateInstance(mapperType!)!;
    }

    [Fact]
    public void ConfigCall_GeneratesListPair_NoPartialNoAttribute()
    {
        // The generator registered the pair purely because of the ForListChildren call site — proof that
        // the config call is the opt-in (previously this returned null and ForListChildren threw).
        System.Runtime.CompilerServices.RuntimeHelpers.RunModuleConstructor(typeof(Invoice).Module.ModuleHandle);

        var projection = ShiftEntityMapperRegistry.FindPairListProjection(typeof(InvoiceLine), typeof(InvoiceLineListDTO));

        Assert.NotNull(projection);
    }

    [Fact]
    public void MapToList_ForListChildren_ProjectsLines_AndProductSelectDTO()
    {
        var mapper = ResolveInvoiceMapper();
        ((IShiftMapperConfigurable<Invoice, InvoiceListDTO, InvoiceDTO>)mapper)
            .AddConfiguration(map => map.ForListChildren(d => d.InvoiceLines, e => e.InvoiceLines));

        var invoices = new[]
        {
            new Invoice
            {
                ManualReference = "INV-1",
                InvoiceNo = 1,
                InvoiceLines = new HashSet<InvoiceLine>
                {
                    new InvoiceLine
                    {
                        ID = 100,
                        Description = "Widget",
                        Price = 9.5m,
                        ProductID = 5,
                        Product = new Product { ID = 5, Name = "Super Widget" },
                    },
                },
            },
        }.AsQueryable();

        var row = mapper.MapToList(invoices).Single();

        // level 2 — the lines are projected into the list row (scalars).
        var line = Assert.Single(row.InvoiceLines);
        Assert.Equal("100", line.ID);
        Assert.Equal("Widget", line.Description);
        Assert.Equal(9.5m, line.Price);

        // level 3 — each line's product is a ShiftEntitySelectDTO: id from the FK, name from the navigation.
        Assert.NotNull(line.Product);
        Assert.Equal("5", line.Product.Value);
        Assert.Equal("Super Widget", line.Product.Text);
    }
}
