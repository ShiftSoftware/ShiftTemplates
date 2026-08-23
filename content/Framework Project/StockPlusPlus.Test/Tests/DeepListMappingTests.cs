using System.Linq;
using ShiftSoftware.ShiftEntity.Core;
using StockPlusPlus.Data.Entities;
using StockPlusPlus.Shared.DTOs.Invoice;

namespace StockPlusPlus.Test.Tests;

/// <summary>
/// DEEP LIST mapping, the EXPLICIT per-level form. The (InvoiceLine, InvoiceLineListDTO) and
/// (Product, InvoiceLineProductListDTO) pairs are generated purely from the ForListChildren / nested
/// ForChild CALLS (config-driven opt-in — no partial, no attribute), and that call's callback (a
/// direction-scoped ShiftListChildMapper) can customize the child's own properties via For.
/// <para>
/// Scoped to the <c>InvoiceListDTO</c> triple on purpose. Automatic deep composition in the list direction
/// landed with Stage A and is exercised by the <c>InvoiceDeepListDTO</c> triple, which builds three levels
/// from a bare <c>UseGeneratedMapper()</c> — see <see cref="DeepListTranslationTests"/>. An explicit
/// <c>ForListChild(ren)</c> takes precedence over the automatic composition for that member, which is what
/// these tests pin.
/// </para>
/// <para>
/// These tests run LINQ-to-objects, so they assert VALUES — which <c>ToQueryString()</c> cannot. The
/// translation suite is the other half and does not replace this one: LINQ-to-objects happily executes
/// constructs EF cannot translate, so a green run here says nothing about SQL.
/// </para>
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

    private static Invoice[] SampleInvoices() => new[]
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
                    Product = new Product { ID = 5, Name = "Super Widget", Price = 120 },
                },
            },
        },
    };

    [Fact]
    public void ConfigCalls_GenerateBothPairs_NoPartialNoAttribute()
    {
        // Both the line pair AND the grandchild product pair are registered purely because of the
        // ForListChildren / nested ForChild calls in the config below — no [ShiftEntityMapper].
        System.Runtime.CompilerServices.RuntimeHelpers.RunModuleConstructor(typeof(Invoice).Module.ModuleHandle);

        Assert.NotNull(ShiftEntityMapperRegistry.FindPairListProjection(typeof(InvoiceLine), typeof(InvoiceLineListDTO)));
        Assert.NotNull(ShiftEntityMapperRegistry.FindPairListProjection(typeof(Product), typeof(InvoiceLineProductListDTO)));
    }

    [Fact]
    public void MapToList_ExplicitChild_ComposesCustomProduct_AndAppliesCustomNameMapping()
    {
        var mapper = ResolveInvoiceMapper();
        ((IShiftMapperConfigurable<Invoice, InvoiceListDTO, InvoiceDTO>)mapper)
            .AddConfiguration(map => map.ForListChildren(d => d.InvoiceLines, e => e.InvoiceLines, line =>
                line.ForChild(l => l.Product, il => il.Product, product =>
                    product.For(p => p.Name, prod => prod.Name + " + Custom Mapping"))));

        var row = mapper.MapToList(SampleInvoices().AsQueryable()).Single();

        var listLine = Assert.Single(row.InvoiceLines);
        Assert.Equal("100", listLine.ID);
        Assert.Equal("Widget", listLine.Description);
        Assert.Equal(9.5m, listLine.Price);

        // The explicitly-composed custom product DTO, with the per-property customization applied.
        Assert.NotNull(listLine.Product);
        Assert.Equal("5", listLine.Product.ID);
        Assert.Equal("Super Widget + Custom Mapping", listLine.Product.Name);   // For customization
        Assert.Equal(120, listLine.Product.Price);                             // convention, untouched
    }

    [Fact]
    public void MapToList_WithoutExplicitChild_LeavesProductNull()
    {
        // No nested ForChild → the product object is NOT composed. Nothing goes deep automatically
        // in the list direction; the line's own scalar columns still project.
        var mapper = ResolveInvoiceMapper();
        ((IShiftMapperConfigurable<Invoice, InvoiceListDTO, InvoiceDTO>)mapper)
            .AddConfiguration(map => map.ForListChildren(d => d.InvoiceLines, e => e.InvoiceLines));

        var row = mapper.MapToList(SampleInvoices().AsQueryable()).Single();

        var listLine = Assert.Single(row.InvoiceLines);
        Assert.Equal("Widget", listLine.Description);
        Assert.Null(listLine.Product);
    }
}
