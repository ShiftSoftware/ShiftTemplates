using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using StockPlusPlus.Data.Entities;
using StockPlusPlus.Shared.DTOs.Invoice;

namespace StockPlusPlus.Test.Tests;

/// <summary>
/// DB-independent tests for DEEP (child) mapping — Invoice ↔ InvoiceLines. No mapper class is declared
/// anywhere: the source generator auto-discovers the (InvoiceLine, InvoiceLineDTO) pair from InvoiceDTO
/// and (a) composes it automatically into the auto Invoice triple mapper's MapToView, and (b) exposes it
/// via ForEntityChildren for the write side (which the production InvoiceRepository uses). End-to-end CRUD
/// through the repository is covered by ManualMappingTests' Invoice tests.
/// </summary>
public class DeepMappingTests
{
    private static IShiftEntityMapper<Invoice, InvoiceListDTO, InvoiceDTO> ResolveInvoiceMapper()
    {
        System.Runtime.CompilerServices.RuntimeHelpers.RunModuleConstructor(typeof(Invoice).Module.ModuleHandle);

        var mapperType = ShiftEntityMapperRegistry.Find(typeof(Invoice), typeof(InvoiceListDTO), typeof(InvoiceDTO));
        Assert.NotNull(mapperType);

        return (IShiftEntityMapper<Invoice, InvoiceListDTO, InvoiceDTO>)Activator.CreateInstance(mapperType!)!;
    }

    private static IShiftObjectMapper<InvoiceLine, InvoiceLineDTO> ResolveLinePair()
    {
        System.Runtime.CompilerServices.RuntimeHelpers.RunModuleConstructor(typeof(Invoice).Module.ModuleHandle);

        var pairType = ShiftEntityMapperRegistry.FindPair(typeof(InvoiceLine), typeof(InvoiceLineDTO));
        Assert.NotNull(pairType);

        return (IShiftObjectMapper<InvoiceLine, InvoiceLineDTO>)Activator.CreateInstance(pairType!)!;
    }

    [Fact]
    public void MapToView_ExplicitChildCollection_ComposesViaForViewChildren()
    {
        // The view direction is now EXPLICIT too — the child collection composes only because ForViewChildren
        // says so (this is exactly what the production InvoiceRepository wires).
        var invoice = new Invoice
        {
            ManualReference = "INV-1",
            InvoiceLines = new HashSet<InvoiceLine>
            {
                new InvoiceLine { Description = "Widget", Price = 9.5m, ProductID = 5 },
            },
        };

        var mapper = ResolveInvoiceMapper();
        ((IShiftMapperConfigurable<Invoice, InvoiceListDTO, InvoiceDTO>)mapper)
            .AddConfiguration(map => map.ForViewChildren(d => d.InvoiceLines, e => e.InvoiceLines));

        var dto = mapper.MapToView(invoice);

        Assert.NotNull(dto.InvoiceLines);
        var line = Assert.Single(dto.InvoiceLines);
        Assert.Equal("Widget", line.Description);
        Assert.Equal(9.5m, line.Price);
        Assert.NotNull(line.Product);
        Assert.Equal("5", line.Product!.Value);        // SelectDTO leaf still maps by convention
    }

    [Fact]
    public void MapToView_WithoutForViewChildren_LeavesChildCollectionEmpty()
    {
        // No ForViewChildren → nothing goes deep automatically; the collection keeps its DTO default.
        var invoice = new Invoice
        {
            ManualReference = "INV-1b",
            InvoiceLines = new HashSet<InvoiceLine> { new InvoiceLine { Description = "Widget", ProductID = 5 } },
        };

        var dto = ResolveInvoiceMapper().MapToView(invoice);

        Assert.Empty(dto.InvoiceLines);
    }

    [Fact]
    public void MapToView_ExplicitChild_NullCollection_YieldsNull()
    {
        var invoice = new Invoice { ManualReference = "INV-2", InvoiceLines = null! };

        var mapper = ResolveInvoiceMapper();
        ((IShiftMapperConfigurable<Invoice, InvoiceListDTO, InvoiceDTO>)mapper)
            .AddConfiguration(map => map.ForViewChildren(d => d.InvoiceLines, e => e.InvoiceLines));

        var dto = mapper.MapToView(invoice);

        Assert.Null(dto.InvoiceLines);
    }

    [Fact]
    public void MapToView_ForViewChildren_CustomizesChildProperty()
    {
        // Customize a property of the deep view object — the same ForView you use on the parent, on the child.
        var invoice = new Invoice
        {
            ManualReference = "INV-3",
            InvoiceLines = new HashSet<InvoiceLine> { new InvoiceLine { Description = "Widget", Price = 9.5m, ProductID = 5 } },
        };

        var mapper = ResolveInvoiceMapper();
        ((IShiftMapperConfigurable<Invoice, InvoiceListDTO, InvoiceDTO>)mapper)
            .AddConfiguration(map => map.ForViewChildren(d => d.InvoiceLines, e => e.InvoiceLines,
                line => line.ForView(l => l.Description, il => il.Description + " (view)")));

        var dto = mapper.MapToView(invoice);

        var line = Assert.Single(dto.InvoiceLines!);
        Assert.Equal("Widget (view)", line.Description);   // deep view customization applied
        Assert.Equal("5", line.Product!.Value);            // other conventions untouched
    }

    [Fact]
    public void MapToEntity_ForEntityChildren_CustomizesChildProperty()
    {
        System.Runtime.CompilerServices.RuntimeHelpers.RunModuleConstructor(typeof(Invoice).Module.ModuleHandle);

        // Same nested-config shape on the WRITE direction: customize a child entity property.
        var builder = new ShiftMapperBuilder<Invoice, InvoiceListDTO, InvoiceDTO>();
        builder.ForEntityChildren(x => x.InvoiceLines, d => d.InvoiceLines,
            line => line.ForEntity(il => il.Description, l => l.Description + " (persisted)"));

        builder.TryGetEntityValue(nameof(Invoice.InvoiceLines), out var value);
        var func = (Func<InvoiceDTO, IServiceProvider?, ICollection<InvoiceLine>?>)value!;

        var dto = new InvoiceDTO
        {
            InvoiceLines = new List<InvoiceLineDTO>
            {
                new InvoiceLineDTO { Description = "A", Price = 1m, Product = new ShiftEntitySelectDTO { Value = "3" } },
            },
        };

        var line = Assert.Single(func(dto, null)!.ToList());
        Assert.Equal("A (persisted)", line.Description);   // deep entity customization applied
        Assert.Equal(3, line.ProductID);                   // convention untouched
    }

    [Fact]
    public void Pair_Map_AppliesConventions()
    {
        var line = new InvoiceLine { Description = "Bolt", Price = 2m, ProductID = 7 };

        var dto = ResolveLinePair().Map(line);

        Assert.Equal("Bolt", dto.Description);
        Assert.Equal(2m, dto.Price);
        Assert.Equal("7", dto.Product.Value);          // FK → SelectDTO convention
    }

    [Fact]
    public void Pair_MapBack_UsesConventions()
    {
        var dto = new InvoiceLineDTO
        {
            Description = "Nut",
            Price = 1m,
            Product = new ShiftEntitySelectDTO { Value = "12" },
        };

        var entity = ResolveLinePair().MapBack(dto, new InvoiceLine());

        Assert.Equal("Nut", entity.Description);
        Assert.Equal(1m, entity.Price);
        Assert.Equal(12, entity.ProductID);            // SelectDTO → FK
    }

    [Fact]
    public void ForEntityChildren_ResolvesPair_AndMapsBackReplaceWithNew()
    {
        System.Runtime.CompilerServices.RuntimeHelpers.RunModuleConstructor(typeof(Invoice).Module.ModuleHandle);

        // The exact wiring the production InvoiceRepository uses.
        var builder = new ShiftMapperBuilder<Invoice, InvoiceListDTO, InvoiceDTO>();
        builder.ForEntityChildren(x => x.InvoiceLines, d => d.InvoiceLines);

        Assert.True(builder.TryGetEntityValue(nameof(Invoice.InvoiceLines), out var value));
        var func = (Func<InvoiceDTO, IServiceProvider?, ICollection<InvoiceLine>?>)value!;

        var dto = new InvoiceDTO
        {
            InvoiceLines = new List<InvoiceLineDTO>
            {
                new InvoiceLineDTO { Description = "A", Price = 1m, Product = new ShiftEntitySelectDTO { Value = "3" } },
                new InvoiceLineDTO { Description = "B", Price = 2m, Product = new ShiftEntitySelectDTO { Value = "4" } },
            },
        };

        var entities = func(dto, null)!.ToList();

        Assert.Equal(2, entities.Count);
        Assert.Equal("A", entities[0].Description);
        Assert.Equal(3, entities[0].ProductID);
        Assert.Equal("B", entities[1].Description);
        Assert.Equal(4, entities[1].ProductID);
        Assert.Equal(0, entities[0].ID);   // replace-with-new: fresh instances
    }
}
