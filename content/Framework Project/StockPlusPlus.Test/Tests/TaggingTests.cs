using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Core.Tagging;
using ShiftSoftware.ShiftEntity.EFCore.Tagging;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using ShiftSoftware.ShiftEntity.Model.Dtos.Tagging;
using StockPlusPlus.Data.DbContext;
using StockPlusPlus.Data.Entities;
using StockPlusPlus.Data.Repositories;
using StockPlusPlus.Shared.DTOs.Product;
using StockPlusPlus.Shared.Enums;

namespace StockPlusPlus.Test.Tests;

/// <summary>
/// Integration tests for the tagging feature: Tag vocabulary CRUD, M:N auto-wiring on
/// IShiftEntityTaggable entities, and the upsert-on-save pipeline including the
/// AutoCreateIfAuthorized policy.
/// </summary>
[Collection("API Collection")]
public class TaggingTests
{
    private readonly CustomWebApplicationFactory factory;

    public TaggingTests(CustomWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public void TaggingServices_AreRegistered()
    {
        using var scope = factory.Services.CreateScope();

        var options = scope.ServiceProvider.GetService<IOptions<ShiftTaggingOptions>>()?.Value;
        var repo = scope.ServiceProvider.GetService<ShiftTagRepository<DB>>();

        Assert.NotNull(options);
        Assert.True(options!.Enabled);
        Assert.NotNull(repo);
        Assert.NotNull(options.Action);
    }

    [Fact]
    public async Task TagEntity_IsPartOfModel_WhenTaggingRegistered()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DB>();

        // Sanity: Tag table should be queryable when tagging is on.
        var anyTag = await db.Tags.AsNoTracking().FirstOrDefaultAsync();
        // No assertion on the result — the call succeeding (no "entity not in model") is the point.
        Assert.True(true);
    }

    [Fact]
    public async Task Tag_CRUD_RoundTripsThroughRepository()
    {
        using var scope = factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<ShiftTagRepository<DB>>();

        // Create
        var dto = new TagDTO
        {
            Name = $"Urgent-{Guid.NewGuid():N}".Substring(0, 16),
            Color = "#FF0000",
            Description = "Time-critical items",
        };

        var entity = new Tag();
        repo.Add(entity);
        var inserted = await repo.UpsertAsync(entity, dto, ActionTypes.Insert,
            userId: null, idempotencyKey: null,
            disableDefaultDataLevelAccess: true, disableGlobalFilters: true);
        await repo.SaveChangesAsync();

        Assert.True(inserted.ID > 0);

        // View round-trip
        var refetched = await repo.FindAsync(inserted.ID, asOf: null,
            disableDefaultDataLevelAccess: true, disableGlobalFilters: true);
        Assert.NotNull(refetched);
        Assert.Equal(dto.Name, refetched!.Name);
        Assert.Equal("#FF0000", refetched.Color);
    }

    [Fact]
    public async Task Product_InsertWithExistingTags_AttachesTagsToProduct()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DB>();
        var brandRepo = scope.ServiceProvider.GetRequiredService<ProductBrandRepository>();
        var categoryRepo = scope.ServiceProvider.GetRequiredService<ProductCategoryRepository>();
        var productRepo = scope.ServiceProvider.GetRequiredService<ProductRepository>();
        var tagRepo = scope.ServiceProvider.GetRequiredService<ShiftTagRepository<DB>>();

        // Setup brand, category
        var brand = new ProductBrand { Name = $"Brand-{Guid.NewGuid():N}".Substring(0, 16) };
        brandRepo.Add(brand);
        await brandRepo.SaveChangesAsync();

        var category = new ProductCategory { Name = $"Cat-{Guid.NewGuid():N}".Substring(0, 16) };
        categoryRepo.Add(category);
        await categoryRepo.SaveChangesAsync();

        // Pre-create two tags in the vocabulary
        var tagSeed1 = new TagDTO { Name = $"Seasonal-{Guid.NewGuid():N}".Substring(0, 18) };
        var seedEntity1 = new Tag();
        tagRepo.Add(seedEntity1);
        var seasonal = await tagRepo.UpsertAsync(seedEntity1, tagSeed1, ActionTypes.Insert, null, null, true, true);

        var tagSeed2 = new TagDTO { Name = $"Featured-{Guid.NewGuid():N}".Substring(0, 18) };
        var seedEntity2 = new Tag();
        tagRepo.Add(seedEntity2);
        var featured = await tagRepo.UpsertAsync(seedEntity2, tagSeed2, ActionTypes.Insert, null, null, true, true);

        await tagRepo.SaveChangesAsync();

        // Insert Product with both existing tags referenced
        var productDto = new ProductDTO
        {
            Name = "Tagged Product",
            TrackingMethod = TrackingMethod.Batch_LOT,
            ProductBrand = new ShiftEntitySelectDTO { Value = brand.ID.ToString(), Text = brand.Name },
            ProductCategory = new ShiftEntitySelectDTO { Value = category.ID.ToString(), Text = category.Name },
            Tags = new List<TagDTO>
            {
                new() { ID = seasonal.ID.ToString(), Name = seasonal.Name },
                new() { ID = featured.ID.ToString(), Name = featured.Name },
            }
        };

        var product = new Product();
        productRepo.Add(product);
        var inserted = await productRepo.UpsertAsync(product, productDto, ActionTypes.Insert, null, null, true, true);
        await productRepo.SaveChangesAsync();

        // Re-fetch to verify M:N persisted
        db.ChangeTracker.Clear();
        var found = await db.Set<Product>()
            .Include(p => p.Tags)
            .FirstOrDefaultAsync(p => p.ID == inserted.ID);

        Assert.NotNull(found);
        Assert.Equal(2, found!.Tags.Count);
        Assert.Contains(found.Tags, t => t.ID == seasonal.ID);
        Assert.Contains(found.Tags, t => t.ID == featured.ID);
    }

    [Fact]
    public async Task Product_OdataList_ProjectsTagsOntoListDto()
    {
        // The list grid shows tags via ProductListDTO.Tags, populated by the mapper's MapToList
        // projection. This verifies that data path (ShiftList then auto-renders the column).
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DB>();
        var brandRepo = scope.ServiceProvider.GetRequiredService<ProductBrandRepository>();
        var categoryRepo = scope.ServiceProvider.GetRequiredService<ProductCategoryRepository>();
        var productRepo = scope.ServiceProvider.GetRequiredService<ProductRepository>();
        var tagRepo = scope.ServiceProvider.GetRequiredService<ShiftTagRepository<DB>>();

        var brand = new ProductBrand { Name = $"LB-{Guid.NewGuid():N}".Substring(0, 17) };
        brandRepo.Add(brand);
        await brandRepo.SaveChangesAsync();

        var category = new ProductCategory { Name = $"LC-{Guid.NewGuid():N}".Substring(0, 17) };
        categoryRepo.Add(category);
        await categoryRepo.SaveChangesAsync();

        var tagSeed = new Tag();
        tagRepo.Add(tagSeed);
        var tag = await tagRepo.UpsertAsync(tagSeed,
            new TagDTO { Name = $"LX-{Guid.NewGuid():N}".Substring(0, 18), Color = "#0099FF" },
            ActionTypes.Insert, null, null, true, true);
        await tagRepo.SaveChangesAsync();

        var productName = $"ListTagged-{Guid.NewGuid():N}".Substring(0, 22);
        var productDto = new ProductDTO
        {
            Name = productName,
            TrackingMethod = TrackingMethod.Batch_LOT,
            ProductBrand = new ShiftEntitySelectDTO { Value = brand.ID.ToString(), Text = brand.Name },
            ProductCategory = new ShiftEntitySelectDTO { Value = category.ID.ToString(), Text = category.Name },
            Tags = new List<TagDTO> { new() { ID = tag.ID.ToString(), Name = tag.Name } }
        };

        var product = new Product();
        productRepo.Add(product);
        var inserted = await productRepo.UpsertAsync(product, productDto, ActionTypes.Insert, null, null, true, true);
        await productRepo.SaveChangesAsync();

        db.ChangeTracker.Clear();

        // The OData list path the grid consumes — MapToList projects Tags onto the list DTO.
        // Bypass default data-level-access (no HTTP user in this scope, same as the other
        // tagging tests) so the just-inserted product is visible; we're exercising the
        // projection, not DLA.
        var queryable = await productRepo.GetIQueryable(
            asOf: null, includes: null, disableDefaultDataLevelAccess: true, disableGlobalFilters: true);
        var listQuery = await productRepo.OdataList(queryable);
        var row = listQuery.FirstOrDefault(p => p.Name == productName);

        Assert.NotNull(row);
        Assert.NotNull(row!.Tags);
        Assert.Single(row.Tags);
        Assert.Equal(tag.Name, row.Tags[0].Name);
        Assert.Equal("#0099FF", row.Tags[0].Color);
    }

    [Fact]
    public async Task Product_InsertWithFreeTypedUnknownTag_IsRejected_WhenUserLacksTagsWrite()
    {
        // Verifies the AutoCreateIfAuthorized policy: in this test scope there is no
        // bearer-token user (the factory grants no Tags.Write), so submitting a free-typed
        // unknown tag should be rejected with a Forbidden ShiftEntityException — preventing
        // unprivileged callers from silently growing the vocabulary.
        //
        // The success path (auto-create when the saving user *does* have Tags.Write) is
        // exercised end-to-end via the HTTP-level test suite, which authenticates first.

        using var scope = factory.Services.CreateScope();
        var brandRepo = scope.ServiceProvider.GetRequiredService<ProductBrandRepository>();
        var categoryRepo = scope.ServiceProvider.GetRequiredService<ProductCategoryRepository>();
        var productRepo = scope.ServiceProvider.GetRequiredService<ProductRepository>();

        var brand = new ProductBrand { Name = $"Brand2-{Guid.NewGuid():N}".Substring(0, 17) };
        brandRepo.Add(brand);
        await brandRepo.SaveChangesAsync();

        var category = new ProductCategory { Name = $"Cat2-{Guid.NewGuid():N}".Substring(0, 17) };
        categoryRepo.Add(category);
        await categoryRepo.SaveChangesAsync();

        var productDto = new ProductDTO
        {
            Name = "Free-typed Tag Product",
            TrackingMethod = TrackingMethod.Batch_LOT,
            ProductBrand = new ShiftEntitySelectDTO { Value = brand.ID.ToString(), Text = brand.Name },
            ProductCategory = new ShiftEntitySelectDTO { Value = category.ID.ToString(), Text = category.Name },
            Tags = new List<TagDTO>
            {
                new() { Name = $"UnknownTag-{Guid.NewGuid():N}".Substring(0, 22) }
            }
        };

        var product = new Product();
        productRepo.Add(product);

        var ex = await Assert.ThrowsAsync<ShiftEntityException>(async () =>
            await productRepo.UpsertAsync(product, productDto, ActionTypes.Insert, null, null, true, true));

        Assert.Equal((int)System.Net.HttpStatusCode.Forbidden, ex.HttpStatusCode);
    }

    [Fact]
    public async Task TagOData_ContainsFilter_ReturnsMatches()
    {
        // Exercises the same OData path that ShiftAutocomplete uses for tag suggestions:
        // GET /api/tags?$filter=contains(Name,'…'). No bespoke autocomplete endpoint — the
        // standard MapShiftEntitySecureCrud list endpoint handles it.
        using var scope = factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<ShiftTagRepository<DB>>();

        var uniqueToken = $"Z{Guid.NewGuid():N}".Substring(0, 8);

        async Task SeedTag(string name)
        {
            var e = new Tag();
            repo.Add(e);
            await repo.UpsertAsync(e, new TagDTO { Name = name }, ActionTypes.Insert, null, null, true, true);
        }

        await SeedTag($"{uniqueToken}-prefix");
        await SeedTag($"middle-{uniqueToken}-tag");
        await SeedTag("unrelated");
        await repo.SaveChangesAsync();

        var query = await repo.OdataList(null);
        var results = query.Where(t => t.Name!.Contains(uniqueToken)).ToList();

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Contains(uniqueToken, r.Name));
        Assert.DoesNotContain("unrelated", results.Select(r => r.Name));
    }

    [Fact]
    public async Task Product_InsertWithTags_ViewAsyncReturnsDtoWithTags()
    {
        // Mirrors the EXACT HTTP POST order in ShiftEntityCrudHandler.PostAsync:
        //   newItem = UpsertAsync(new Entity(), dto, Insert, ...);   // pipeline runs on UNTRACKED entity
        //   repository.Add(newItem);                                  // tracking starts AFTER pipeline
        //   SaveChangesAsync();
        //   ViewAsync(newItem);                                       // response body
        // The previous test had Add → Upsert, which masks the bug because the entity is
        // tracked when the pipeline mutates its M:N skip navigation.
        using var scope = factory.Services.CreateScope();
        var brandRepo = scope.ServiceProvider.GetRequiredService<ProductBrandRepository>();
        var categoryRepo = scope.ServiceProvider.GetRequiredService<ProductCategoryRepository>();
        var productRepo = scope.ServiceProvider.GetRequiredService<ProductRepository>();
        var tagRepo = scope.ServiceProvider.GetRequiredService<ShiftTagRepository<DB>>();

        var brand = new ProductBrand { Name = $"VB-{Guid.NewGuid():N}".Substring(0, 17) };
        brandRepo.Add(brand);
        await brandRepo.SaveChangesAsync();

        var category = new ProductCategory { Name = $"VC-{Guid.NewGuid():N}".Substring(0, 17) };
        categoryRepo.Add(category);
        await categoryRepo.SaveChangesAsync();

        var tagSeed = new Tag();
        tagRepo.Add(tagSeed);
        var seasonal = await tagRepo.UpsertAsync(
            tagSeed,
            new TagDTO { Name = $"Hot-{Guid.NewGuid():N}".Substring(0, 18) },
            ActionTypes.Insert, null, null, true, true);
        await tagRepo.SaveChangesAsync();

        var productDto = new ProductDTO
        {
            Name = "View After Insert",
            TrackingMethod = TrackingMethod.Batch_LOT,
            ProductBrand = new ShiftEntitySelectDTO { Value = brand.ID.ToString(), Text = brand.Name },
            ProductCategory = new ShiftEntitySelectDTO { Value = category.ID.ToString(), Text = category.Name },
            Tags = new List<TagDTO>
            {
                new() { ID = seasonal.ID.ToString(), Name = seasonal.Name }
            }
        };

        // Exact HTTP POST order: Upsert(new Entity()) first, Add() after.
        var inserted = await productRepo.UpsertAsync(entity: new Product(), dto: productDto, actionType: ActionTypes.Insert,
            userId: null, idempotencyKey: null, disableDefaultDataLevelAccess: true, disableGlobalFilters: true);
        productRepo.Add(inserted);
        await productRepo.SaveChangesAsync();

        // The HTTP POST handler calls ViewAsync(newItem) right after this.
        var viewDto = await productRepo.ViewAsync(inserted);

        Assert.NotNull(viewDto.Tags);
        Assert.Single(viewDto.Tags);
        Assert.Equal(seasonal.Name, viewDto.Tags[0].Name);
        Assert.Equal(seasonal.ID.ToString(), viewDto.Tags[0].ID);
    }

    [Fact]
    public async Task Product_UpdateWithTags_ViewAsyncReturnsDtoWithTags()
    {
        // Mirrors the HTTP PUT flow: FindAsync → UpsertAsync → SaveChangesAsync → ViewAsync.
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DB>();
        var brandRepo = scope.ServiceProvider.GetRequiredService<ProductBrandRepository>();
        var categoryRepo = scope.ServiceProvider.GetRequiredService<ProductCategoryRepository>();
        var productRepo = scope.ServiceProvider.GetRequiredService<ProductRepository>();
        var tagRepo = scope.ServiceProvider.GetRequiredService<ShiftTagRepository<DB>>();

        var brand = new ProductBrand { Name = $"VUB-{Guid.NewGuid():N}".Substring(0, 17) };
        brandRepo.Add(brand);
        await brandRepo.SaveChangesAsync();

        var category = new ProductCategory { Name = $"VUC-{Guid.NewGuid():N}".Substring(0, 17) };
        categoryRepo.Add(category);
        await categoryRepo.SaveChangesAsync();

        async Task<Tag> SeedTag(string prefix)
        {
            var e = new Tag();
            tagRepo.Add(e);
            return await tagRepo.UpsertAsync(e, new TagDTO { Name = $"{prefix}-{Guid.NewGuid():N}".Substring(0, 18) },
                ActionTypes.Insert, null, null, true, true);
        }

        var tagA = await SeedTag("UA");
        var tagB = await SeedTag("UB");
        await tagRepo.SaveChangesAsync();

        // Insert with one tag.
        var insertDto = new ProductDTO
        {
            Name = "View After Update",
            TrackingMethod = TrackingMethod.Batch_LOT,
            ProductBrand = new ShiftEntitySelectDTO { Value = brand.ID.ToString(), Text = brand.Name },
            ProductCategory = new ShiftEntitySelectDTO { Value = category.ID.ToString(), Text = category.Name },
            Tags = new List<TagDTO> { new() { ID = tagA.ID.ToString(), Name = tagA.Name } }
        };
        var product = new Product();
        productRepo.Add(product);
        var inserted = await productRepo.UpsertAsync(product, insertDto, ActionTypes.Insert, null, null, true, true);
        await productRepo.SaveChangesAsync();

        // Fresh tracker simulates the PUT request arriving in a new request scope.
        db.ChangeTracker.Clear();

        var loaded = await productRepo.FindAsync(inserted.ID, asOf: null,
            disableDefaultDataLevelAccess: true, disableGlobalFilters: true);
        Assert.NotNull(loaded);

        var updateDto = new ProductDTO
        {
            ID = loaded!.ID.ToString(),
            Name = "View After Update — Modified",
            TrackingMethod = TrackingMethod.Batch_LOT,
            ProductBrand = new ShiftEntitySelectDTO { Value = brand.ID.ToString(), Text = brand.Name },
            ProductCategory = new ShiftEntitySelectDTO { Value = category.ID.ToString(), Text = category.Name },
            Tags = new List<TagDTO>
            {
                new() { ID = tagA.ID.ToString(), Name = tagA.Name },
                new() { ID = tagB.ID.ToString(), Name = tagB.Name },
            }
        };

        await productRepo.UpsertAsync(loaded, updateDto, ActionTypes.Update, null, null, true, true);
        await productRepo.SaveChangesAsync();

        // What the HTTP PUT handler sends back to the client:
        var viewDto = await productRepo.ViewAsync(loaded);

        Assert.NotNull(viewDto.Tags);
        Assert.Equal(2, viewDto.Tags.Count);
        Assert.Contains(viewDto.Tags, t => t.ID == tagA.ID.ToString());
        Assert.Contains(viewDto.Tags, t => t.ID == tagB.ID.ToString());
    }

    [Fact]
    public async Task Product_UpdateTags_ReplacesTagSet()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DB>();
        var brandRepo = scope.ServiceProvider.GetRequiredService<ProductBrandRepository>();
        var categoryRepo = scope.ServiceProvider.GetRequiredService<ProductCategoryRepository>();
        var productRepo = scope.ServiceProvider.GetRequiredService<ProductRepository>();
        var tagRepo = scope.ServiceProvider.GetRequiredService<ShiftTagRepository<DB>>();

        var brand = new ProductBrand { Name = $"Brand3-{Guid.NewGuid():N}".Substring(0, 17) };
        brandRepo.Add(brand);
        await brandRepo.SaveChangesAsync();

        var category = new ProductCategory { Name = $"Cat3-{Guid.NewGuid():N}".Substring(0, 17) };
        categoryRepo.Add(category);
        await categoryRepo.SaveChangesAsync();

        // Seed three tags
        async Task<Tag> SeedTag(string name)
        {
            var e = new Tag();
            tagRepo.Add(e);
            return await tagRepo.UpsertAsync(e, new TagDTO { Name = name }, ActionTypes.Insert, null, null, true, true);
        }

        var t1 = await SeedTag($"T1-{Guid.NewGuid():N}".Substring(0, 14));
        var t2 = await SeedTag($"T2-{Guid.NewGuid():N}".Substring(0, 14));
        var t3 = await SeedTag($"T3-{Guid.NewGuid():N}".Substring(0, 14));
        await tagRepo.SaveChangesAsync();

        // Insert product with [t1, t2]
        var productDto = new ProductDTO
        {
            Name = "Tag Update Test",
            TrackingMethod = TrackingMethod.Batch_LOT,
            ProductBrand = new ShiftEntitySelectDTO { Value = brand.ID.ToString(), Text = brand.Name },
            ProductCategory = new ShiftEntitySelectDTO { Value = category.ID.ToString(), Text = category.Name },
            Tags = new List<TagDTO>
            {
                new() { ID = t1.ID.ToString(), Name = t1.Name },
                new() { ID = t2.ID.ToString(), Name = t2.Name },
            }
        };

        var product = new Product();
        productRepo.Add(product);
        var inserted = await productRepo.UpsertAsync(product, productDto, ActionTypes.Insert, null, null, true, true);
        await productRepo.SaveChangesAsync();

        // Now update to [t2, t3] — t1 should be detached, t3 attached
        db.ChangeTracker.Clear();
        var reloaded = await productRepo.FindAsync(inserted.ID, asOf: null,
            disableDefaultDataLevelAccess: true, disableGlobalFilters: true);
        Assert.NotNull(reloaded);

        productDto.Tags = new List<TagDTO>
        {
            new() { ID = t2.ID.ToString(), Name = t2.Name },
            new() { ID = t3.ID.ToString(), Name = t3.Name },
        };
        await productRepo.UpsertAsync(reloaded!, productDto, ActionTypes.Update, null, null, true, true);
        await productRepo.SaveChangesAsync();

        // Verify final tag set
        db.ChangeTracker.Clear();
        var verify = await db.Set<Product>().Include(p => p.Tags).FirstOrDefaultAsync(p => p.ID == inserted.ID);
        Assert.NotNull(verify);
        Assert.Equal(2, verify!.Tags.Count);
        Assert.Contains(verify.Tags, t => t.ID == t2.ID);
        Assert.Contains(verify.Tags, t => t.ID == t3.ID);
        Assert.DoesNotContain(verify.Tags, t => t.ID == t1.ID);
    }
}
