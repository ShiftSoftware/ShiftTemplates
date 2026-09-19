using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using ShiftMapper;
using StockPlusPlus.Data.Entities;
using StockPlusPlus.Data.Repositories;
using StockPlusPlus.Shared.DTOs.Invoice;

namespace StockPlusPlus.Test.Tests;

/// <summary>
/// DEEP LIST mapping: the list projection composes <c>InvoiceListDTO.InvoiceLines</c> and, inside each line, the
/// custom <c>InvoiceLineProductListDTO</c>, from the pairs ShiftMapper declares below the repository's list map —
/// no partial, no attribute, no configuration. The same map carries the ONE customization the repository wrote
/// (<c>Total</c>). <c>api/invoice-deep</c> builds three levels the same way (<see cref="DeepListTranslationTests"/>
/// pins its SQL).
/// <para>
/// These tests run LINQ-to-objects, so they assert VALUES — which <c>ToQueryString()</c> cannot. The
/// translation suite is the other half and does not replace this one: LINQ-to-objects happily executes
/// constructs EF cannot translate, so a green run here says nothing about SQL.
/// </para>
/// </summary>
[Collection("API Collection")]
public class DeepListMappingTests
{
    private readonly CustomWebApplicationFactory factory;

    public DeepListMappingTests(CustomWebApplicationFactory factory) => this.factory = factory;

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
    public void MapToList_ComposesTheLines_AndTheProductInsideThem()
    {
        using var scope = factory.Services.CreateScope();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        var row = mapper.ProjectTo<Invoice, InvoiceListDTO>(SampleInvoices().AsQueryable()).Single();

        var listLine = Assert.Single(row.InvoiceLines);
        Assert.Equal("100", listLine.ID);
        Assert.Equal("Widget", listLine.Description);
        Assert.Equal(9.5m, listLine.Price);

        // The grandchild: a custom product DTO (not a select), composed from the navigation.
        Assert.NotNull(listLine.Product);
        Assert.Equal("5", listLine.Product.ID);
        Assert.Equal("Super Widget", listLine.Product.Name);
        Assert.Equal(120, listLine.Product.Price);

        // The repository's customization rides on the same map.
        Assert.Equal(9.5m, row.Total);
    }

    [Fact]
    public void MapToList_ThroughTheRepository_IsTheSameProjection()
    {
        using var scope = factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<InvoiceRepository>();

        var row = repo.MapToList(SampleInvoices().AsQueryable()).Single();

        Assert.Equal("Super Widget", Assert.Single(row.InvoiceLines).Product.Name);
        Assert.Equal(9.5m, row.Total);
    }

    [Fact]
    public void MapToList_ThreeLevels_ForTheDeepEndpoint()
    {
        using var scope = factory.Services.CreateScope();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        var invoice = SampleInvoices()[0];
        invoice.InvoiceLines.First().Product.ProductBrand = new ProductBrand { ID = 9, Name = "Acme" };

        var row = mapper.ProjectTo<Invoice, InvoiceDeepListDTO>(new[] { invoice }.AsQueryable()).Single();

        var line = Assert.Single(row.InvoiceLines);
        Assert.Equal("Super Widget", line.Product.Name);
        Assert.Equal("Acme", line.Product.ProductBrand.Name);   // depth 3, nothing configured
    }
}
