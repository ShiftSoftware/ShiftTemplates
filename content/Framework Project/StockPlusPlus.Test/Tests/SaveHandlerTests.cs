using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Model.HashIds;
using StockPlusPlus.Data.DbContext;
using StockPlusPlus.Data.Entities;
using StockPlusPlus.Data.SaveHandlers;
using StockPlusPlus.Shared.DTOs.Product;
using StockPlusPlus.Shared.DTOs.ProductCategory;
using StockPlusPlus.Shared.Enums;
using System.Net;
using System.Net.Http.Json;

namespace StockPlusPlus.Test.Tests;

/// <summary>
/// The sample's save handlers, one per stage, through the API or the context and against SQL Server:
/// <list type="bullet">
/// <item>before save: <see cref="InvoiceNumbering"/>, the next invoice number under a lock;</item>
/// <item>after save: <see cref="ProductCategoryCode"/>, a code made from the new ID in the same transaction;</item>
/// <item>before commit: <see cref="NoDraftProductsOnInvoices"/>, a rule over everything the transaction wrote;</item>
/// <item>after commit: <see cref="ProductPriceFeed"/>, telling another system once the change is committed.</item>
/// </list>
/// </summary>
[Collection("API Collection")]
public class SaveHandlerTests(CustomWebApplicationFactory factory)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    // ── Before save: the invoice number ─────────────────────────────────────────

    [Fact]
    public async Task A_new_invoice_gets_the_next_number_of_its_company()
    {
        var product = await AProductAsync("Numbered");
        using var client = factory.CreateClient();

        var first = await PostInvoiceAsync(client, "NUMBERED-1", product.Key);
        var second = await PostInvoiceAsync(client, "NUMBERED-2", product.Key);

        Assert.Equal(first.InvoiceNo + 1, second.InvoiceNo);

        // What was answered is what was written.
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DB>();

        Assert.Equal(second.InvoiceNo, await db.Invoices.Where(x => x.ManualReference == "NUMBERED-2").Select(x => x.InvoiceNo).SingleAsync(Ct));
    }

    [Fact]
    public async Task A_number_the_client_sends_with_a_new_invoice_is_not_taken()
    {
        var product = await AProductAsync("Client number");
        using var client = factory.CreateClient();

        var before = await PostInvoiceAsync(client, "CLIENT-NUMBER-1", product.Key);

        var json = InvoiceJson("CLIENT-NUMBER-2", product.Key);
        json["InvoiceNo"] = 999_999_999;

        using var response = await client.PostAsJsonAsync("api/Invoice", json, Ct);
        var entity = Entity(await ExpectAsync(response, HttpStatusCode.Created));

        Assert.Equal(before.InvoiceNo + 1, entity["InvoiceNo"]!.GetValue<long>());
    }

    [Fact]
    public async Task An_invoice_saved_through_the_context_and_not_through_the_api_is_numbered_too()
    {
        using var client = factory.CreateClient();
        var product = await AProductAsync("Seeded");
        var before = await PostInvoiceAsync(client, "SEEDED-0", product.Key);

        // The handler is declared with the context, so a seeder or a job that saves through it gets the same numbers.
        // Two invoices in one save, and a third in a second save of the same transaction: they follow each other.
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DB>();

        var seeded = new[] { "SEEDED-1", "SEEDED-2", "SEEDED-3" }
            .Select(reference => new Invoice { ManualReference = reference, InvoiceDate = DateTimeOffset.UtcNow })
            .ToArray();

        await using (var transaction = await db.Database.BeginTransactionAsync(Ct))
        {
            db.Invoices.AddRange(seeded[0], seeded[1]);
            await db.SaveChangesAsync(Ct);

            db.Invoices.Add(seeded[2]);
            await db.SaveChangesAsync(Ct);

            await transaction.CommitAsync(Ct);
        }

        Assert.Equal(
            [before.InvoiceNo + 1, before.InvoiceNo + 2, before.InvoiceNo + 3],
            seeded.Select(invoice => invoice.InvoiceNo).Order());
    }

    [Fact]
    public async Task Invoices_created_at_the_same_time_get_different_numbers_and_none_is_left_out()
    {
        const int writers = 6;
        const int each = 8;

        var product = await AProductAsync("Concurrent");
        using var client = factory.CreateClient();

        // The series has a first number before the writers start, so the expected numbers are known.
        var before = await PostInvoiceAsync(client, "CONCURRENT-0", product.Key);

        var numbers = (await Task.WhenAll(Enumerable.Range(0, writers).Select(writer => Task.Run(async () =>
        {
            var mine = new List<long>();

            for (var i = 0; i < each; i++)
                mine.Add((await PostInvoiceAsync(client, $"CONCURRENT-{writer}-{i}", product.Key)).InvoiceNo);

            return mine;
        }, Ct)))).SelectMany(mine => mine).Order().ToList();

        // Without the lock, two requests read the same last number. With it every number is given once.
        Assert.Equal(Enumerable.Range(1, writers * each).Select(offset => before.InvoiceNo + offset), numbers);
    }

    // ── After save: the category's code ─────────────────────────────────────────

    [Fact]
    public async Task A_category_saved_without_a_code_gets_one_made_from_its_new_id()
    {
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync("api/ProductCategory", await CategoryJsonAsync("Coded by the save", code: null), Ct);
        var entity = Entity(await ExpectAsync(response, HttpStatusCode.Created));

        using var scope = factory.Services.CreateScope();
        var id = scope.ServiceProvider.GetRequiredService<IHashIdService>().Decode<ProductCategoryDTO>(entity["ID"]!.GetValue<string>());

        // The answer already carries the code: it was written before the save call returned.
        Assert.Equal($"CAT-{id:D5}", entity["Code"]!.GetValue<string>());

        var db = scope.ServiceProvider.GetRequiredService<DB>();
        Assert.Equal($"CAT-{id:D5}", await db.ProductCategories.Where(x => x.ID == id).Select(x => x.Code).SingleAsync(Ct));
    }

    [Fact]
    public async Task A_category_saved_with_a_code_keeps_it()
    {
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync("api/ProductCategory", await CategoryJsonAsync("Coded by a person", code: "MINE"), Ct);
        var entity = Entity(await ExpectAsync(response, HttpStatusCode.Created));

        Assert.Equal("MINE", entity["Code"]!.GetValue<string>());
    }

    // ── Before commit: no draft product on an invoice ───────────────────────────

    [Fact]
    public async Task An_invoice_with_a_line_for_a_draft_product_is_not_committed_and_takes_no_number()
    {
        var published = await AProductAsync("Published");
        var draft = await AProductAsync("Still a draft", draft: true);
        using var client = factory.CreateClient();

        var before = await PostInvoiceAsync(client, "DRAFT-0", published.Key);

        using (var refused = await client.PostAsJsonAsync("api/Invoice", InvoiceJson("DRAFT-1", published.Key, draft.Key), Ct))
        {
            var body = JsonNode.Parse(await ExpectAsync(refused, HttpStatusCode.BadRequest))!;

            Assert.Equal("Draft product", body["Message"]!["Title"]!.GetValue<string>());
            Assert.Equal("An invoice cannot have a line for a draft product: Still a draft.", body["Message"]!["Body"]!.GetValue<string>());
        }

        // The invoice and its lines were written, and the rule undid them: the transaction did not commit.
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DB>();

            Assert.False(await db.Invoices.AnyAsync(x => x.ManualReference == "DRAFT-1", Ct));
            Assert.False(await db.InvoiceLines.AnyAsync(x => x.ProductID == draft.Id, Ct));
        }

        // The number it had been given went with it.
        var after = await PostInvoiceAsync(client, "DRAFT-2", published.Key);
        Assert.Equal(before.InvoiceNo + 1, after.InvoiceNo);
    }

    [Fact]
    public async Task A_product_that_is_published_before_the_transaction_commits_may_be_on_its_invoice()
    {
        var draft = await AProductAsync("Published in time", draft: true);

        // Two saves of one transaction: the invoice first, then the product. The rule looks when both are written.
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DB>();

        await using (var transaction = await db.Database.BeginTransactionAsync(Ct))
        {
            db.Invoices.Add(AnInvoice("PUBLISHED-IN-TIME", draft.Id));
            await db.SaveChangesAsync(Ct);

            (await db.Products.SingleAsync(x => x.ID == draft.Id, Ct)).IsDraft = false;
            await db.SaveChangesAsync(Ct);

            await transaction.CommitAsync(Ct);
        }

        Assert.True(await db.Invoices.AnyAsync(x => x.ManualReference == "PUBLISHED-IN-TIME", Ct));
    }

#if shiftFrameworkDevelopmentMode
    // ── On SQLite ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task On_SQLite_too_invoices_are_numbered_without_a_gap_and_a_draft_product_is_refused()
    {
        const int writers = 4;
        const int each = 5;

        // The sample also runs on SQLite (Settings:Database). A shared database in memory, as the API uses, and for each
        // save a context with a connection of its own.
        var options = new DbContextOptionsBuilder<DB>()
            .UseSqlite($"Data Source=save-handlers-{Guid.NewGuid():N};Mode=Memory;Cache=Shared").Options;

        var published = new Product { Name = "Published" };
        var draft = new Product { Name = "Still a draft", IsDraft = true };

        await using (var db = new DB(options))
        {
            await db.Database.EnsureCreatedAsync(Ct);

            published.ProductBrand = draft.ProductBrand = new ProductBrand { Name = "Brand" };
            published.ProductCategory = draft.ProductCategory = new ProductCategory { Name = "Category" };

            db.Products.AddRange(published, draft);
            await db.SaveChangesAsync(Ct);
        }

        async Task<long> SaveAsync(string reference, params long[] products)
        {
            await using var db = new DB(options);
            var invoice = AnInvoice(reference, products);

            db.Invoices.Add(invoice);
            await db.SaveChangesAsync(Ct);

            return invoice.InvoiceNo;
        }

        Assert.Equal(1, await SaveAsync("SQLITE-1", published.ID));

        var refused = await Assert.ThrowsAsync<ShiftEntityException>(() => SaveAsync("SQLITE-DRAFT", published.ID, draft.ID));
        Assert.Equal("An invoice cannot have a line for a draft product: Still a draft.", refused.Message.Body);

        // At the same time, and after a save that gave its number back.
        var numbers = (await Task.WhenAll(Enumerable.Range(0, writers).Select(writer => Task.Run(async () =>
        {
            var mine = new List<long>();

            for (var i = 0; i < each; i++)
                mine.Add(await SaveAsync($"SQLITE-{writer}-{i}", published.ID));

            return mine;
        }, Ct)))).SelectMany(mine => mine).Order();

        Assert.Equal(Enumerable.Range(2, writers * each).Select(number => (long)number), numbers);
    }
#endif

    // ── After commit: the price feed ────────────────────────────────────────────

    [Fact]
    public async Task A_committed_price_change_reaches_the_price_feed_once_with_the_old_and_the_new_price()
    {
        var feed = factory.Services.GetRequiredService<SamplePriceFeed>();
        using var client = factory.CreateClient();
        var name = "Priced " + Guid.NewGuid().ToString("N");

        using var created = await client.PostAsJsonAsync("api/Product", await ProductJsonAsync(name, price: 100), Ct);
        var product = Entity(await ExpectAsync(created, HttpStatusCode.Created));
        var key = product["ID"]!.GetValue<string>();

        // A new product is not a price change.
        Assert.DoesNotContain(feed.Published, change => change.Name == name);

        product["Price"] = 120;

        using var changed = await client.PutAsJsonAsync($"api/Product/{key}", product, Ct);
        product = Entity(await ExpectAsync(changed, HttpStatusCode.OK));

        var told = Assert.Single(feed.Published, change => change.Name == name);
        Assert.Equal(100, told.OldPrice);
        Assert.Equal(120, told.NewPrice);

        // A save that leaves the price as it was tells nobody.
        product["Name"] = name + " renamed";

        using var renamed = await client.PutAsJsonAsync($"api/Product/{key}", product, Ct);
        await ExpectAsync(renamed, HttpStatusCode.OK);

        Assert.Single(feed.Published, change => change.ProductID == told.ProductID);
    }

    // ── What the tests post ─────────────────────────────────────────────────────

    private async Task<(long Id, string Key)> AProductAsync(string name, bool draft = false)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DB>();

        var product = new Product
        {
            Name = name,
            IsDraft = draft,
            ProductBrand = new ProductBrand { Name = name + " brand" },
            ProductCategory = new ProductCategory { Name = name + " category" },
        };

        db.Products.Add(product);
        await db.SaveChangesAsync(Ct);

        return (product.ID, scope.ServiceProvider.GetRequiredService<IHashIdService>().Encode<ProductDTO>(product.ID));
    }

    private static Invoice AnInvoice(string reference, params long[] products) => new()
    {
        ManualReference = reference,
        InvoiceDate = DateTimeOffset.UtcNow,
        InvoiceLines = products.Select(product => new InvoiceLine { Description = "A line", Price = 10, ProductID = product }).ToList(),
    };

    private static JsonObject InvoiceJson(string reference, params string[] productKeys) => new()
    {
        ["ManualReference"] = reference,
        ["InvoiceDate"] = DateTimeOffset.UtcNow,
        ["InvoiceLines"] = new JsonArray(productKeys.Select(key => (JsonNode)new JsonObject
        {
            ["Description"] = "A line",
            ["Price"] = 10m,
            ["Product"] = new JsonObject { ["Value"] = key },
        }).ToArray()),
    };

    private static async Task<(string Key, long InvoiceNo)> PostInvoiceAsync(HttpClient client, string reference, string productKey)
    {
        using var response = await client.PostAsJsonAsync("api/Invoice", InvoiceJson(reference, productKey), Ct);
        var entity = Entity(await ExpectAsync(response, HttpStatusCode.Created));

        return (entity["ID"]!.GetValue<string>(), entity["InvoiceNo"]!.GetValue<long>());
    }

    private async Task<JsonObject> CategoryJsonAsync(string name, string? code)
    {
        using var scope = factory.Services.CreateScope();
        var hashIds = scope.ServiceProvider.GetRequiredService<IHashIdService>();

        return new JsonObject
        {
            ["Name"] = name,
            ["Code"] = code,
            ["TrackingMethod"] = TrackingMethod.NoTracking.ToString(),
            // A brand of the identity module. Nothing but its ID is kept on the category.
            ["Brand"] = new JsonObject { ["Value"] = hashIds.Encode(1, new BrandHashIdConverter()) },
        };
    }

    private async Task<JsonObject> ProductJsonAsync(string name, int price)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DB>();
        var hashIds = scope.ServiceProvider.GetRequiredService<IHashIdService>();

        var brand = new ProductBrand { Name = name + " brand" };
        var category = new ProductCategory { Name = name + " category" };

        db.AddRange(brand, category);
        await db.SaveChangesAsync(Ct);

        return new JsonObject
        {
            ["Name"] = name,
            ["Price"] = price,
            ["TrackingMethod"] = TrackingMethod.NoTracking.ToString(),
            ["ProductCategory"] = new JsonObject { ["Value"] = hashIds.Encode<ProductCategoryDTO>(category.ID) },
            ["ProductBrand"] = new JsonObject { ["Value"] = brand.ID.ToString() },
        };
    }

    private static JsonObject Entity(string responseBody) => JsonNode.Parse(responseBody)!["Entity"]!.AsObject();

    private static async Task<string> ExpectAsync(HttpResponseMessage response, HttpStatusCode expected)
    {
        var body = await response.Content.ReadAsStringAsync(Ct);

        Assert.True(response.StatusCode == expected,
            $"Expected HTTP {(int)expected} ({expected}), got {(int)response.StatusCode} ({response.StatusCode}). Body: {body}");

        return body;
    }
}
