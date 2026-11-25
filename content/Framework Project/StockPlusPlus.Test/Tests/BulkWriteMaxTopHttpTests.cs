using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftIdentity.Core;
using ShiftSoftware.TypeAuth.Core;
using StockPlusPlus.Data.DbContext;
using StockPlusPlus.Data.Entities;
using StockPlusPlus.Shared.ActionTrees;
using StockPlusPlus.Shared.DTOs.Product;
using StockPlusPlus.Shared.Enums;
using System.Net;
using System.Net.Http.Json;

namespace StockPlusPlus.Test.Tests;

/// <summary>
/// The product list's bulk delete over HTTP: a user deletes at most GeneralActionTree.DataGridBulkWriteMaxTop products in
/// one request, and a bigger request changes nothing.
/// </summary>
[Collection("API Collection")]
public class BulkWriteMaxTopHttpTests
{
    private readonly CustomWebApplicationFactory factory;

    public BulkWriteMaxTopHttpTests(CustomWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task ASelectionOverTheLimit_IsRefusedAndNothingIsDeleted()
    {
        var ids = await AddProductsAsync(4);
        using var client = Client(limit: 3);

        using var response = await DeleteAsync(client, Selected(ids));

        await AssertRefusedAsync(response, rows: 4, limit: 3);
        Assert.Equal(0, await CountDeletedAsync(ids));
    }

    [Fact]
    public async Task ASelectionAtTheLimit_IsDeleted()
    {
        var ids = await AddProductsAsync(3);
        using var client = Client(limit: 3);

        using var response = await DeleteAsync(client, Selected(ids));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(3, await CountDeletedAsync(ids));
    }

    [Fact]
    public async Task AUserWithoutTheGrant_IsHeldTo5Rows()
    {
        var ids = await AddProductsAsync(6);
        using var client = Client(limit: null);

        using (var refused = await DeleteAsync(client, Selected(ids)))
            await AssertRefusedAsync(refused, rows: 6, limit: 5);

        Assert.Equal(0, await CountDeletedAsync(ids));

        using var deleted = await DeleteAsync(client, Selected(ids.Take(5)));

        Assert.Equal(HttpStatusCode.OK, deleted.StatusCode);
        Assert.Equal(5, await CountDeletedAsync(ids));
    }

    // The request says "all rows" and understates their number, which SelectStateDTO.Count would repeat.
    [Fact]
    public async Task AllRowsOfAFilter_AreCountedByTheServer()
    {
        var name = $"Bulk write {Guid.NewGuid():N}";
        var ids = await AddProductsAsync(4, name);
        var all = new { All = true, Items = Array.Empty<object>(), Total = 1, Filter = $"Name eq '{name}'" };

        using (var client = Client(limit: 3))
        using (var refused = await client.PostAsJsonAsync("api/product/bulk-delete", all))
            await AssertRefusedAsync(refused, rows: 4, limit: 3);

        Assert.Equal(0, await CountDeletedAsync(ids));

        using (var client = Client(limit: 4))
        using (var deleted = await client.PostAsJsonAsync("api/product/bulk-delete", all))
            Assert.Equal(HttpStatusCode.OK, deleted.StatusCode);

        Assert.Equal(4, await CountDeletedAsync(ids));
    }

    // The limit is not a permission.
    [Fact]
    public async Task AHighLimit_DoesNotLetAUserDeleteWithoutDeleteAccess()
    {
        var ids = await AddProductsAsync(1);
        using var client = Client(limit: 100, Access.Read, Access.Write);

        using var response = await DeleteAsync(client, Selected(ids));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(0, await CountDeletedAsync(ids));
    }

    private HttpClient Client(int? limit, params Access[] productAccess)
    {
        if (productAccess.Length == 0)
            productAccess = [Access.Read, Access.Write, Access.Delete];

        var tree = new Dictionary<string, object>
        {
            [nameof(StockPlusPlusActionTree)] = new Dictionary<string, object> { [nameof(StockPlusPlusActionTree.Product)] = productAccess },
            // Without data-level access the user reaches no product, and a row out of reach doesn't count.
            [nameof(ShiftIdentityActions)] = new Dictionary<string, object>
            {
                [nameof(ShiftIdentityActions.DataLevelAccess)] = new[] { Access.Read, Access.Write, Access.Delete, Access.Maximum },
            },
        };

        if (limit is not null)
        {
            tree[nameof(GeneralActionTree)] = new Dictionary<string, object>
            {
                [nameof(GeneralActionTree.DataGridBulkWriteMaxTop)] = limit.Value,
            };
        }

        return factory.CreateAuthenticatedClient(JsonSerializer.Serialize(tree));
    }

    private static Task<HttpResponseMessage> DeleteAsync(HttpClient client, object selection)
        => client.PostAsJsonAsync("api/product/bulk-delete", selection);

    private object Selected(IEnumerable<long> ids)
    {
        using var scope = factory.Services.CreateScope();
        var hashIds = scope.ServiceProvider.GetRequiredService<IHashIdService>();

        return new { All = false, Items = ids.Select(x => new { ID = hashIds.Encode<ProductListDTO>(x) }).ToArray(), Total = 0 };
    }

    private async Task<List<long>> AddProductsAsync(int count, string name = "Bulk write")
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DB>();
        var brand = new ProductBrand { Name = "Bulk write" };
        var category = new ProductCategory { Name = "Bulk write" };

        var products = Enumerable.Range(0, count).Select(_ => new Product
        {
            Name = name,
            TrackingMethod = TrackingMethod.Batch_LOT,
            ReleaseDate = DateTimeOffset.UtcNow,
            ProductBrand = brand,
            ProductCategory = category,
        }).ToList();

        db.AddRange(products);
        await db.SaveChangesAsync();

        return products.Select(x => x.ID).ToList();
    }

    private async Task<int> CountDeletedAsync(List<long> ids)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DB>();

        return await db.Set<Product>().CountAsync(x => ids.Contains(x.ID) && x.IsDeleted);
    }

    private static async Task AssertRefusedAsync(HttpResponseMessage response, int rows, int limit)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        // The response ActionButton shows to the user.
        var body = await response.Content.ReadFromJsonAsync<ShiftEntityResponse<ProductListDTO>>();

        Assert.Equal("Bulk Write Limit Exceeded", body!.Message!.Title);
        Assert.Contains($"({rows})", body.Message.Body);
        Assert.Contains($"maximum of {limit} ", body.Message.Body);
    }
}
