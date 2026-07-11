using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using StockPlusPlus.Data.DbContext;
using StockPlusPlus.Data.Entities;
using StockPlusPlus.Data.Mappers;
using StockPlusPlus.Data.Repositories;
using StockPlusPlus.Shared.DTOs.ProductBrand;

namespace StockPlusPlus.Test.Tests;

/// <summary>
/// DB-independent unit tests for the SOURCE-GENERATED mapper: ProductBrandMapper is a
/// [ShiftEntityMapper] partial class whose four methods are produced by the ShiftEntity
/// source generator at compile time.
/// </summary>
public class SourceGeneratedMapperTests
{
    [Fact]
    public void MapToView_MapsScalars_ForeignKey_AndBaseFields()
    {
        var entity = new ProductBrand
        {
            Name = "Gen Brand",
            Description = "Generated mapping",
            Code = "GB-01",
            TeamID = 42,
            CreateDate = new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero),
        };

        var dto = new ProductBrandMapper().MapToView(entity);

        Assert.Equal("Gen Brand", dto.Name);
        Assert.Equal("Generated mapping", dto.Description);
        Assert.Equal("GB-01", dto.Code);

        // FK → ShiftEntitySelectDTO (no Team navigation on the entity, so no Text)
        Assert.NotNull(dto.Team);
        Assert.Equal("42", dto.Team!.Value);

        // Audit/base fields via MapBaseFields
        Assert.Equal(entity.ID.ToString(), dto.ID);
        Assert.Equal(entity.CreateDate, dto.CreateDate);
        Assert.False(dto.IsDeleted);
    }

    [Fact]
    public void MapToView_NullForeignKey_YieldsNullSelectDTO()
    {
        var dto = new ProductBrandMapper().MapToView(new ProductBrand { Name = "No Team", TeamID = null });

        Assert.Null(dto.Team);
    }

    [Fact]
    public void MapToEntity_MapsScalars_AndNullableForeignKey()
    {
        var existing = new ProductBrand();

        new ProductBrandMapper().MapToEntity(new ProductBrandDTO
        {
            Name = "Updated",
            Description = "New description",
            Code = "UP-01",
            Team = new ShiftEntitySelectDTO { Value = "7" },
        }, existing);

        Assert.Equal("Updated", existing.Name);
        Assert.Equal("New description", existing.Description);
        Assert.Equal("UP-01", existing.Code);
        Assert.Equal(7, existing.TeamID);
    }

    [Fact]
    public void MapToEntity_NullTeam_SetsNullForeignKey()
    {
        var existing = new ProductBrand { TeamID = 99 };

        new ProductBrandMapper().MapToEntity(new ProductBrandDTO { Name = "X", Team = null }, existing);

        Assert.Null(existing.TeamID);
    }

    [Fact]
    public void MapToList_ProjectsScalars_IdsAndNullableLongsAsStrings()
    {
        var brands = new[]
        {
            new ProductBrand { Name = "A", Description = "dA", Code = "cA", TeamID = 5 },
            new ProductBrand { Name = "B", Description = "dB", Code = "cB", TeamID = null },
        }.AsQueryable();

        var list = new ProductBrandMapper().MapToList(brands).ToList();

        Assert.Equal(2, list.Count);
        Assert.Equal("A", list[0].Name);
        Assert.Equal("dA", list[0].Description);
        Assert.Equal("cA", list[0].Code);
        Assert.Equal("5", list[0].TeamID);
        Assert.Null(list[1].TeamID);
    }

    [Fact]
    public void CopyEntity_ShallowCopies_PreservingReloadAfterSave()
    {
        var source = new ProductBrand { Name = "Fresh", Description = "From DB", TeamID = 3 };
        var target = new ProductBrand { Name = "Stale", ReloadAfterSave = true };

        new ProductBrandMapper().CopyEntity(source, target);

        Assert.Equal("Fresh", target.Name);
        Assert.Equal("From DB", target.Description);
        Assert.Equal(3, target.TeamID);
        Assert.True(target.ReloadAfterSave); // intentionally NOT copied
    }
}

/// <summary>
/// End-to-end integration tests: ProductBrandRepository plugs the source-generated mapper via
/// options.UseMapper(new ProductBrandMapper()), so CRUD flows through generated code.
/// </summary>
[Collection("API Collection")]
public class SourceGeneratedMappingTests
{
    private readonly CustomWebApplicationFactory factory;

    public SourceGeneratedMappingTests(CustomWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task ProductBrand_InsertAndView_ThroughGeneratedMapper()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DB>();
        var brandRepo = scope.ServiceProvider.GetRequiredService<ProductBrandRepository>();

        var dto = new ProductBrandDTO
        {
            Name = "SourceGen Brand",
            Description = "Inserted through the generated mapper",
            Code = "SG-001",
            Team = null,
        };

        var entity = new ProductBrand();
        brandRepo.Add(entity);
        await brandRepo.UpsertAsync(entity, dto, ActionTypes.Insert, userId: null, idempotencyKey: null, disableDefaultDataLevelAccess: true, disableGlobalFilters: true);
        await brandRepo.SaveChangesAsync();

        db.ChangeTracker.Clear();

        var found = (await brandRepo.FindAsync(entity.ID, asOf: null, disableDefaultDataLevelAccess: true, disableGlobalFilters: true))!;
        var viewDto = await brandRepo.ViewAsync(found);

        Assert.Equal("SourceGen Brand", viewDto.Name);
        Assert.Equal("Inserted through the generated mapper", viewDto.Description);
        Assert.Equal("SG-001", viewDto.Code);
        Assert.Null(viewDto.Team);

        Assert.NotNull(viewDto.ID);
        Assert.NotEqual(default, viewDto.CreateDate);
        Assert.False(viewDto.IsDeleted);
    }

    [Fact]
    public async Task ProductBrand_Update_ThroughGeneratedMapper()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DB>();
        var brandRepo = scope.ServiceProvider.GetRequiredService<ProductBrandRepository>();

        var entity = new ProductBrand();
        brandRepo.Add(entity);
        await brandRepo.UpsertAsync(entity, new ProductBrandDTO { Name = "Before Gen Update", Code = "BG-01" }, ActionTypes.Insert, userId: null, idempotencyKey: null, disableDefaultDataLevelAccess: true, disableGlobalFilters: true);
        await brandRepo.SaveChangesAsync();

        db.ChangeTracker.Clear();

        var found = (await brandRepo.FindAsync(entity.ID, asOf: null, disableDefaultDataLevelAccess: true, disableGlobalFilters: true))!;
        await brandRepo.UpsertAsync(found, new ProductBrandDTO { Name = "After Gen Update", Code = "AG-01" }, ActionTypes.Update, userId: null, idempotencyKey: null, disableDefaultDataLevelAccess: true, disableGlobalFilters: true);
        await brandRepo.SaveChangesAsync();

        db.ChangeTracker.Clear();

        var updated = (await brandRepo.FindAsync(entity.ID, asOf: null, disableDefaultDataLevelAccess: true, disableGlobalFilters: true))!;
        var viewDto = await brandRepo.ViewAsync(updated);

        Assert.Equal("After Gen Update", viewDto.Name);
        Assert.Equal("AG-01", viewDto.Code);
    }

    [Fact]
    public async Task ProductBrand_MapToList_ProjectsCorrectly()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DB>();
        var brandRepo = scope.ServiceProvider.GetRequiredService<ProductBrandRepository>();

        var entity = new ProductBrand { Name = "SourceGen List Brand", Description = "List projection", Code = "SGL-01" };
        brandRepo.Add(entity);
        await brandRepo.SaveChangesAsync();

        db.ChangeTracker.Clear();

        var entityQuery = await brandRepo.GetIQueryable(asOf: null, includes: null, disableDefaultDataLevelAccess: true, disableGlobalFilters: true);
        var queryable = await brandRepo.OdataList(entityQuery);
        var listItems = await queryable.Where(x => x.Name == "SourceGen List Brand").ToListAsync();

        Assert.Single(listItems);
        var item = listItems[0];
        Assert.Equal("SourceGen List Brand", item.Name);
        Assert.Equal("List projection", item.Description);
        Assert.Equal("SGL-01", item.Code);
        Assert.Equal(entity.ID.ToString(), item.ID);
    }
}
