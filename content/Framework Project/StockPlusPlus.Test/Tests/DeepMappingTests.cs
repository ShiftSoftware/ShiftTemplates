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
        // View AUTO-composes the child collection; ForViewChildren is the explicit OVERRIDE path and
        // composes the same InvoiceLine → InvoiceLineDTO pair. Both yield the composed collection.
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
    public void MapToView_AutoComposesChildCollection()
    {
        // Zero-code: the generated MapToView auto-composes the InvoiceLines child collection through the
        // auto (InvoiceLine, InvoiceLineDTO) pair — no ForViewChildren needed.
        var invoice = new Invoice
        {
            ManualReference = "INV-1b",
            InvoiceLines = new HashSet<InvoiceLine> { new InvoiceLine { Description = "Widget", Price = 3m, ProductID = 5 } },
        };

        var dto = ResolveInvoiceMapper().MapToView(invoice);

        var line = Assert.Single(dto.InvoiceLines);
        Assert.Equal("Widget", line.Description);
        Assert.Equal(3m, line.Price);
        Assert.Equal("5", line.Product.Value);   // FK → SelectDTO convention, composed automatically
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

    // NOTE: per-property customization of a DEEP child via RUNTIME nested config (line => line.ForView/ForEntity)
    // is no longer honored for baked members — the generator decides custom-vs-convention at BUILD time from the
    // static config it can see. Deep-child customization is now expressed statically (in the repo/mapper Configure)
    // and baked; BuildTimeMappingTests covers the baked custom/ignore/auto-compose behavior. Composition itself
    // (parent-level ForViewChildren / replace-with-new ForEntityChildren) is still covered by the tests above/below.

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
        var func = (Func<InvoiceDTO, MappingContext, ICollection<InvoiceLine>?>)value!;

        var dto = new InvoiceDTO
        {
            InvoiceLines = new List<InvoiceLineDTO>
            {
                new InvoiceLineDTO { Description = "A", Price = 1m, Product = new ShiftEntitySelectDTO { Value = "3" } },
                new InvoiceLineDTO { Description = "B", Price = 2m, Product = new ShiftEntitySelectDTO { Value = "4" } },
            },
        };

        var entities = func(dto, default)!.ToList();

        Assert.Equal(2, entities.Count);
        Assert.Equal("A", entities[0].Description);
        Assert.Equal(3, entities[0].ProductID);
        Assert.Equal("B", entities[1].Description);
        Assert.Equal(4, entities[1].ProductID);
        Assert.Equal(0, entities[0].ID);   // replace-with-new: fresh instances
    }
}
