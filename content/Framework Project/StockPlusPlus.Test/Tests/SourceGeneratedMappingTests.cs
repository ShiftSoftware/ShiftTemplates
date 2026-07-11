using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using StockPlusPlus.Data.DbContext;
using StockPlusPlus.Data.Entities;
using StockPlusPlus.Data.Mappers;
using StockPlusPlus.Data.Repositories;
using StockPlusPlus.Shared.DTOs;
using StockPlusPlus.Shared.DTOs.ProductBrand;

namespace StockPlusPlus.Test.Tests;

/// <summary>
/// DB-independent unit tests for the [ShiftEntityMapper] PARTIAL-CLASS form of source generation:
/// ProductBrandMapper is a declared (empty) partial class whose four methods are produced by the
/// ShiftEntity source generator at compile time — the customization path (implement a method in the
/// partial class to take it over).
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
    public void CopyEntity_CopiesProperties_PreservingReloadAfterSave()
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
/// DB-independent unit tests for the ZERO-CODE (auto-discovery) form of source generation: no mapper
/// class is declared anywhere for Country's (Country, CountryGeneratedDTO, CountryGeneratedDTO) triple —
/// the generator discovers it (from CountryRepository and the api/country-generated endpoint attribute),
/// emits an auto-named mapper, and registers it in ShiftEntityMapperRegistry via a module initializer.
/// </summary>
public class AutoDiscoveredGeneratedMapperTests
{
    private static IShiftEntityMapper<Country, CountryGeneratedDTO, CountryGeneratedDTO> ResolveMapper()
    {
        // The registration is a module initializer in the Data assembly; force it, since a test may
        // run before any Data-assembly code has executed.
        System.Runtime.CompilerServices.RuntimeHelpers.RunModuleConstructor(typeof(Country).Module.ModuleHandle);

        var mapperType = ShiftEntityMapperRegistry.Find(typeof(Country), typeof(CountryGeneratedDTO), typeof(CountryGeneratedDTO));
        Assert.NotNull(mapperType);

        return (IShiftEntityMapper<Country, CountryGeneratedDTO, CountryGeneratedDTO>)Activator.CreateInstance(mapperType!)!;
    }

    [Fact]
    public void Registry_ContainsAutoGeneratedMapper_ForTheTriple()
    {
        var mapper = ResolveMapper();

        Assert.StartsWith("Generated_", mapper.GetType().Name);
    }

    [Fact]
    public void MapToView_MapsNameAndBaseFields()
    {
        var entity = new Country
        {
            Name = "Genland",
            CreateDate = new DateTimeOffset(2026, 3, 4, 0, 0, 0, TimeSpan.Zero),
        };

        var dto = ResolveMapper().MapToView(entity);

        Assert.Equal("Genland", dto.Name);
        Assert.Equal(entity.ID.ToString(), dto.ID);
        Assert.Equal(entity.CreateDate, dto.CreateDate);
        Assert.False(dto.IsDeleted);
    }

    [Fact]
    public void MapToEntity_MapsName()
    {
        var existing = new Country { Name = "Old" };

        ResolveMapper().MapToEntity(new CountryGeneratedDTO { Name = "New" }, existing);

        Assert.Equal("New", existing.Name);
    }

    [Fact]
    public void MapToList_ProjectsNameAndId()
    {
        var countries = new[]
        {
            new Country { Name = "Alpha" },
            new Country { Name = "Beta" },
        }.AsQueryable();

        var list = ResolveMapper().MapToList(countries).ToList();

        Assert.Equal(2, list.Count);
        Assert.Equal("Alpha", list[0].Name);
        Assert.Equal("Beta", list[1].Name);
    }
}

/// <summary>
/// End-to-end integration tests for the ZERO-CODE form: CountryRepository opts in via
/// options.UseGeneratedMapper(), which resolves the auto-discovered, auto-generated mapper from the
/// registry — no mapper class is declared anywhere. CRUD flows through generated code.
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
    public async Task Country_InsertAndView_ThroughAutoGeneratedMapper()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DB>();
        var countryRepo = scope.ServiceProvider.GetRequiredService<CountryRepository>();

        var dto = new CountryGeneratedDTO { Name = "SourceGen Country" };

        var entity = new Country();
        countryRepo.Add(entity);
        await countryRepo.UpsertAsync(entity, dto, ActionTypes.Insert, userId: null, idempotencyKey: null, disableDefaultDataLevelAccess: true, disableGlobalFilters: true);
        await countryRepo.SaveChangesAsync();

        db.ChangeTracker.Clear();

        var found = (await countryRepo.FindAsync(entity.ID, asOf: null, disableDefaultDataLevelAccess: true, disableGlobalFilters: true))!;
        var viewDto = await countryRepo.ViewAsync(found);

        Assert.Equal("SourceGen Country", viewDto.Name);
        Assert.NotNull(viewDto.ID);
        Assert.NotEqual(default, viewDto.CreateDate);
        Assert.False(viewDto.IsDeleted);
    }

    [Fact]
    public async Task Country_Update_ThroughAutoGeneratedMapper()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DB>();
        var countryRepo = scope.ServiceProvider.GetRequiredService<CountryRepository>();

        var entity = new Country();
        countryRepo.Add(entity);
        await countryRepo.UpsertAsync(entity, new CountryGeneratedDTO { Name = "Before Gen Update" }, ActionTypes.Insert, userId: null, idempotencyKey: null, disableDefaultDataLevelAccess: true, disableGlobalFilters: true);
        await countryRepo.SaveChangesAsync();

        db.ChangeTracker.Clear();

        var found = (await countryRepo.FindAsync(entity.ID, asOf: null, disableDefaultDataLevelAccess: true, disableGlobalFilters: true))!;
        await countryRepo.UpsertAsync(found, new CountryGeneratedDTO { Name = "After Gen Update" }, ActionTypes.Update, userId: null, idempotencyKey: null, disableDefaultDataLevelAccess: true, disableGlobalFilters: true);
        await countryRepo.SaveChangesAsync();

        db.ChangeTracker.Clear();

        var updated = (await countryRepo.FindAsync(entity.ID, asOf: null, disableDefaultDataLevelAccess: true, disableGlobalFilters: true))!;
        var viewDto = await countryRepo.ViewAsync(updated);

        Assert.Equal("After Gen Update", viewDto.Name);
    }

    [Fact]
    public async Task Country_MapToList_ProjectsCorrectly()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DB>();
        var countryRepo = scope.ServiceProvider.GetRequiredService<CountryRepository>();

        var entity = new Country { Name = "SourceGen List Country" };
        countryRepo.Add(entity);
        await countryRepo.SaveChangesAsync();

        db.ChangeTracker.Clear();

        var entityQuery = await countryRepo.GetIQueryable(asOf: null, includes: null, disableDefaultDataLevelAccess: true, disableGlobalFilters: true);
        var queryable = await countryRepo.OdataList(entityQuery);
        var listItems = await queryable.Where(x => x.Name == "SourceGen List Country").ToListAsync();

        Assert.Single(listItems);
        Assert.Equal("SourceGen List Country", listItems[0].Name);
        Assert.Equal(entity.ID.ToString(), listItems[0].ID);
    }
}
