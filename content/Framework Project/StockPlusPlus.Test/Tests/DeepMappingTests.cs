using Microsoft.Extensions.DependencyInjection;
using ShiftMapper;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using StockPlusPlus.Data.Entities;
using StockPlusPlus.Shared.DTOs.Invoice;

namespace StockPlusPlus.Test.Tests;

/// <summary>
/// DEEP (child) mapping — Invoice ↔ InvoiceLines — with nothing declared for the child anywhere: ShiftMapper
/// declares <c>InvoiceLine ↔ InvoiceLineDTO</c> from the DTO graph below the repository's pairs (ten levels by
/// default), so the view composes the lines, the write direction replaces them with new instances, and the pair
/// itself is an ordinary map any service can run. End-to-end CRUD through the repository is covered by
/// ManualMappingTests' Invoice tests.
/// </summary>
[Collection("API Collection")]
public class DeepMappingTests
{
    private readonly CustomWebApplicationFactory factory;

    public DeepMappingTests(CustomWebApplicationFactory factory) => this.factory = factory;

    private IMapper Door(IServiceScope scope) => scope.ServiceProvider.GetRequiredService<IMapper>();

    [Fact]
    public void MapToView_ComposesTheChildCollection_WithNothingConfigured()
    {
        using var scope = factory.Services.CreateScope();

        var invoice = new Invoice
        {
            ManualReference = "INV-1",
            InvoiceLines = new HashSet<InvoiceLine>
            {
                new InvoiceLine { Description = "Widget", Price = 9.5m, ProductID = 5 },
            },
        };

        var dto = Door(scope).Map<Invoice, InvoiceDTO>(invoice);

        Assert.NotNull(dto.InvoiceLines);
        var line = Assert.Single(dto.InvoiceLines);
        Assert.Equal("Widget", line.Description);
        Assert.Equal(9.5m, line.Price);
        Assert.NotNull(line.Product);
        Assert.Equal("5", line.Product!.Value);   // the select convention, inside the nested map
    }

    /// <summary>A null navigation collection becomes an EMPTY one (Q5 of the plan): the DTO's list is always there.</summary>
    [Fact]
    public void MapToView_NullCollection_YieldsEmpty()
    {
        using var scope = factory.Services.CreateScope();

        var dto = Door(scope).Map<Invoice, InvoiceDTO>(new Invoice { ManualReference = "INV-2", InvoiceLines = null! });

        Assert.NotNull(dto.InvoiceLines);
        Assert.Empty(dto.InvoiceLines);
    }

    [Fact]
    public void ThePair_IsAnOrdinaryMap()
    {
        using var scope = factory.Services.CreateScope();

        var line = new InvoiceLine { Description = "Bolt", Price = 2m, ProductID = 7 };

        var dto = Door(scope).Map<InvoiceLine, InvoiceLineDTO>(line);

        Assert.Equal("Bolt", dto.Description);
        Assert.Equal(2m, dto.Price);
        Assert.Equal("7", dto.Product.Value);
    }

    [Fact]
    public void ThePair_MapsBackByConvention()
    {
        using var scope = factory.Services.CreateScope();

        var dto = new InvoiceLineDTO
        {
            Description = "Nut",
            Price = 1m,
            Product = new ShiftEntitySelectDTO { Value = "12" },
        };

        var entity = Door(scope).Map<InvoiceLineDTO, InvoiceLine>(dto, new InvoiceLine());

        Assert.Equal("Nut", entity.Description);
        Assert.Equal(1m, entity.Price);
        Assert.Equal(12, entity.ProductID);   // select → FK, through the pack's string → long
    }

    /// <summary>
    /// The write direction REPLACES the child collection with new instances (the build says so: SM0049). The
    /// old lines are the repository's business — InvoiceRepository deletes and recreates them in UpsertAsync.
    /// </summary>
    [Fact]
    public void MapToEntity_ReplacesTheChildren_WithNewInstances()
    {
        using var scope = factory.Services.CreateScope();

        var existing = new Invoice
        {
            ID = 10,
            InvoiceLines = new HashSet<InvoiceLine> { new InvoiceLine { ID = 100, Description = "old", ProductID = 1 } },
        };

        var dto = new InvoiceDTO
        {
            ManualReference = "INV-3",
            InvoiceLines = new List<InvoiceLineDTO>
            {
                new InvoiceLineDTO { Description = "A", Price = 1m, Product = new ShiftEntitySelectDTO { Value = "3" } },
                new InvoiceLineDTO { Description = "B", Price = 2m, Product = new ShiftEntitySelectDTO { Value = "4" } },
            },
        };

        Door(scope).Map<InvoiceDTO, Invoice>(dto, existing);

        var lines = existing.InvoiceLines.ToList();
        Assert.Equal(2, lines.Count);
        Assert.Equal("A", lines[0].Description);
        Assert.Equal(3, lines[0].ProductID);
        Assert.Equal("B", lines[1].Description);
        Assert.Equal(4, lines[1].ProductID);
        Assert.Equal(0, lines[0].ID);    // fresh instances: ID is never written from a DTO
        Assert.Equal(10, existing.ID);   // nor the parent's
    }
}
