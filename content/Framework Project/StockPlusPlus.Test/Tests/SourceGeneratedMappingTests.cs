using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ShiftMapper;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.EFCore;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using StockPlusPlus.Data.DbContext;
using StockPlusPlus.Data.Entities;
using StockPlusPlus.Data.Repositories;
using StockPlusPlus.Shared.DTOs;
using StockPlusPlus.Shared.DTOs.Invoice;
using StockPlusPlus.Shared.DTOs.ProductBrand;
using StockPlusPlus.Shared.DTOs.ProductCategory;

namespace StockPlusPlus.Test.Tests;

/// <summary>
/// The MAPPER CLASS door: <c>Mappers/ProductBrandMapper.cs</c> is an ordinary <c>ShiftMapperBase</c> declaring
/// only the two pairs it customizes (the list map's <c>Code</c>, the write map's <c>AfterMap</c>); those replace
/// the automatic maps for their pairs, the other two pairs of the triple stay automatic, and the framework's
/// conventions apply to all four. Everything is asserted through the host's <see cref="IMapper"/> — the same
/// object <c>ProductBrandRepository</c> maps through and any service can inject (as <c>Mapper</c>, for the typed
/// methods, or as <c>IMapper</c>).
/// <para>
/// Member-for-member parity with the old generator is pinned separately, for every triple, by
/// <see cref="RepositoryMappingParityTests"/>; these tests read as the sample's documentation.
/// </para>
/// </summary>
[Collection("API Collection")]
public class MapperClassDoorTests
{
    private readonly CustomWebApplicationFactory factory;

    public MapperClassDoorTests(CustomWebApplicationFactory factory) => this.factory = factory;

    [Fact]
    public void MapToView_MapsScalars_ForeignKey_AndBaseFields()
    {
        using var scope = factory.Services.CreateScope();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        var entity = new ProductBrand
        {
            Name = "Gen Brand",
            Description = "Generated mapping",
            Code = "GB-01",
            TeamID = 42,
            CreateDate = new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero),
        };

        // The view pair is AUTOMATIC: the class declares nothing for it.
        var dto = mapper.Map<ProductBrand, ProductBrandDTO>(entity);

        Assert.Equal("Gen Brand", dto.Name);
        Assert.Equal("Generated mapping", dto.Description);
        Assert.Equal("GB-01", dto.Code);

        // FK → ShiftEntitySelectDTO by the framework's convention (no Team navigation on the entity, so no Text)
        Assert.NotNull(dto.Team);
        Assert.Equal("42", dto.Team!.Value);

        Assert.Equal(entity.ID.ToString(), dto.ID);
        Assert.Equal(entity.CreateDate, dto.CreateDate);
        Assert.False(dto.IsDeleted);
    }

    [Fact]
    public void MapToView_NullForeignKey_YieldsNullSelectDTO()
    {
        using var scope = factory.Services.CreateScope();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        var dto = mapper.Map<ProductBrand, ProductBrandDTO>(new ProductBrand { Name = "No Team", TeamID = null });

        Assert.Null(dto.Team);   // no key, no value
    }

    [Fact]
    public void MapToEntity_MapsScalars_AndNullableForeignKey_ThenRunsTheAfterMap()
    {
        using var scope = factory.Services.CreateScope();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        var existing = new ProductBrand();

        mapper.Map<ProductBrandDTO, ProductBrand>(new ProductBrandDTO
        {
            Name = "Updated",
            Description = "New description",
            Code = "  UP-01  ",
            Team = new ShiftEntitySelectDTO { Value = "7" },
        }, existing);

        Assert.Equal("Updated", existing.Name);
        Assert.Equal("New description", existing.Description);
        Assert.Equal("UP-01", existing.Code);   // the class's AfterMap trims, after every convention ran
        Assert.Equal(7, existing.TeamID);
    }

    [Fact]
    public void MapToEntity_NullTeam_SetsNullForeignKey()
    {
        using var scope = factory.Services.CreateScope();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        var existing = new ProductBrand { TeamID = 99 };

        mapper.Map<ProductBrandDTO, ProductBrand>(new ProductBrandDTO { Name = "X", Team = null }, existing);

        Assert.Null(existing.TeamID);
    }

    [Fact]
    public void MapToList_ProjectsScalars_AndTheClassCustomizesCode()
    {
        using var scope = factory.Services.CreateScope();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        var brands = new[]
        {
            new ProductBrand { Name = "A", Description = "dA", Code = "cA", TeamID = 5 },
            new ProductBrand { Name = "B", Description = "dB", Code = null, TeamID = null },
        }.AsQueryable();

        var list = mapper.ProjectTo<ProductBrand, ProductBrandListDTO>(brands).ToList();

        Assert.Equal(2, list.Count);
        Assert.Equal("A", list[0].Name);
        Assert.Equal("dA", list[0].Description);
        Assert.Equal("cA", list[0].Code);
        Assert.Equal("5", list[0].TeamID);
        Assert.Null(list[1].TeamID);   // an absent key stays absent, in memory and in SQL alike
        Assert.Equal("(No Code)", list[1].Code);   // the class's ForMember, composed into the projection
    }

    [Fact]
    public void CopyEntity_CopiesProperties_PreservingReloadAfterSave()
    {
        using var scope = factory.Services.CreateScope();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        var source = new ProductBrand { Name = "Fresh", Description = "From DB", TeamID = 3 };
        var target = new ProductBrand { Name = "Stale", ReloadAfterSave = true };

        mapper.Map<ProductBrand, ProductBrand>(source, target);

        Assert.Equal("Fresh", target.Name);
        Assert.Equal("From DB", target.Description);
        Assert.Equal(3, target.TeamID);
        Assert.True(target.ReloadAfterSave);   // the pack ignores it in both roles
    }

    [Fact]
    public void TheRepository_MapsThroughTheSameMapper()
    {
        using var scope = factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<ProductBrandRepository>();

        var resolved = Assert.IsType<ShiftMapperEntityMapper<ProductBrand, ProductBrandListDTO, ProductBrandDTO>>(repo.ShiftRepositoryOptions.Mapper);
        Assert.Same(scope.ServiceProvider.GetRequiredService<IMapper>(), resolved.Mapper);

        var list = repo.MapToList(new[] { new ProductBrand { Name = "X", Code = null } }.AsQueryable()).ToList();
        Assert.Equal("(No Code)", list[0].Code);
    }
}

/// <summary>
/// The AUTOMATIC door, with nothing written: the maps a marked endpoint attribute or a
/// <c>ShiftRepository&lt;,,,&gt;</c> closing declares on their own, with the framework's conventions —
/// <c>ShiftEntitySelectDTO</c> ↔ foreign key, files ↔ JSON, hash ids, the members the pipeline owns, and a
/// blank required key answered as a 400 naming the field.
/// </summary>
[Collection("API Collection")]
public class AutomaticMappingTests
{
    private readonly CustomWebApplicationFactory factory;

    public AutomaticMappingTests(CustomWebApplicationFactory factory) => this.factory = factory;

    private IMapper Door(IServiceScope scope) => scope.ServiceProvider.GetRequiredService<IMapper>();

    [Fact]
    public void TheEndpointAttribute_DeclaresAllFourMaps()
    {
        using var scope = factory.Services.CreateScope();
        var mapper = Door(scope);

        // api/country-generated: nothing is written for the triple but the attribute.
        Assert.True(mapper.CanMap(typeof(Country), typeof(CountryGeneratedDTO)));
        Assert.True(mapper.CanMap(typeof(CountryGeneratedDTO), typeof(Country)));
        Assert.True(mapper.CanMap(typeof(Country), typeof(CountryGeneratedDTO)));
        Assert.True(mapper.CanMap(typeof(Country), typeof(Country)));

        // api/countrymapped is the hand-written CountryMapper's: no marker on the WithMapper attribute.
        Assert.False(mapper.CanMap(typeof(Country), typeof(CountryMappedDTO)));
    }

    [Fact]
    public void MapToView_MapsNameAndBaseFields()
    {
        using var scope = factory.Services.CreateScope();

        var entity = new Country
        {
            Name = "Genland",
            CreateDate = new DateTimeOffset(2026, 3, 4, 0, 0, 0, TimeSpan.Zero),
        };

        var dto = Door(scope).Map<Country, CountryGeneratedDTO>(entity);

        // api/country-generated uses ONE DTO type as both list and view, so its entity → DTO map is ONE map:
        // the customization Country.ConfigureRepository writes on m.List is the view's too (Q15 of the plan).
        // A triple that wants the two to differ uses two DTO types, as every other sample triple does.
        Assert.Equal("Genland (via IConfiguresShiftRepository)", dto.Name);
        Assert.Equal(entity.ID.ToString(), dto.ID);
        Assert.Equal(entity.CreateDate, dto.CreateDate);
        Assert.False(dto.IsDeleted);
    }

    [Fact]
    public void MapToEntity_MapsName_AndNeverTheKey()
    {
        using var scope = factory.Services.CreateScope();

        var existing = new Country { ID = 7, Name = "Old" };

        Door(scope).Map<CountryGeneratedDTO, Country>(new CountryGeneratedDTO { ID = "99", Name = "New" }, existing);

        Assert.Equal("New", existing.Name);
        Assert.Equal(7, existing.ID);   // ID is the database's: ignored as a destination by the pack
    }

    [Fact]
    public void MapToView_ConvertsPhotosJsonToFileList_AndBrandFkToSelectDTO()
    {
        using var scope = factory.Services.CreateScope();

        var entity = new ProductCategory
        {
            Name = "Files & FK Category",
            Photos = new List<ShiftFileDTO>
            {
                new ShiftFileDTO { Blob = "photos/cat1.jpg", Name = "cat1.jpg" },
                new ShiftFileDTO { Blob = "photos/cat2.jpg", Name = "cat2.jpg" },
            }.ToJsonString(),
            BrandID = 9,
        };

        var dto = Door(scope).Map<ProductCategory, ProductCategoryDTO>(entity);

        Assert.NotNull(dto.Photos);
        Assert.Equal(2, dto.Photos!.Count);
        Assert.Equal("photos/cat1.jpg", dto.Photos[0].Blob);
        Assert.Equal("cat1.jpg", dto.Photos[0].Name);
        Assert.Equal("photos/cat2.jpg", dto.Photos[1].Blob);

        Assert.NotNull(dto.Brand);
        Assert.Equal("9", dto.Brand!.Value);
    }

    [Fact]
    public void MapToView_NullPhotos_YieldsEmptyList_AndNullBrand_YieldsNullSelectDTO()
    {
        using var scope = factory.Services.CreateScope();

        var dto = Door(scope).Map<ProductCategory, ProductCategoryDTO>(new ProductCategory { Name = "Empty", Photos = null, BrandID = null });

        Assert.NotNull(dto.Photos);
        Assert.Empty(dto.Photos!);
        Assert.Null(dto.Brand);
    }

    [Fact]
    public void MapToEntity_SerializesPhotos_AndParsesBrandFk()
    {
        using var scope = factory.Services.CreateScope();

        var existing = new ProductCategory();

        Door(scope).Map<ProductCategoryDTO, ProductCategory>(new ProductCategoryDTO
        {
            Name = "Upserted",
            Photos = new List<ShiftFileDTO> { new ShiftFileDTO { Blob = "photos/new.jpg", Name = "new.jpg" } },
            Brand = new ShiftEntitySelectDTO { Value = "12" },
        }, existing);

        Assert.NotNull(existing.Photos);
        var roundTripped = existing.Photos.ToShiftFiles();
        Assert.Single(roundTripped!);
        Assert.Equal("photos/new.jpg", roundTripped![0].Blob);
        Assert.Equal("new.jpg", roundTripped[0].Name);

        Assert.Equal(12, existing.BrandID);
    }

    [Fact]
    public void MapToEntity_NullBrand_ClearsTheNullableForeignKey()
    {
        using var scope = factory.Services.CreateScope();

        var existing = new ProductCategory { BrandID = 5 };

        Door(scope).Map<ProductCategoryDTO, ProductCategory>(new ProductCategoryDTO { Name = "X", Brand = null }, existing);

        Assert.Null(existing.BrandID);
    }

    /// <summary>
    /// A blank select on a REQUIRED key is the client's mistake: a 400 naming the field, the same shape
    /// <c>MappingHelpers.ToForeignKey</c> throws — never a silent 0 that saves a row pointing at nothing.
    /// </summary>
    [Fact]
    public void MapToEntity_BlankRequiredForeignKey_IsA400NamingTheSelect()
    {
        using var scope = factory.Services.CreateScope();

        var ex = Assert.Throws<ShiftEntityException>(() =>
            Door(scope).Map<InvoiceLineDTO, InvoiceLine>(new InvoiceLineDTO { Description = "x", Price = 1, Product = new ShiftEntitySelectDTO { Value = "" } }, new InvoiceLine()));

        Assert.Equal(400, ex.HttpStatusCode);
        Assert.Equal("Product", ex.Message.For);
        Assert.Equal("'Product' is required.", ex.Message.Body);
    }
}

/// <summary>
/// End-to-end integration tests for the ZERO-CODE form: <c>CountryRepository</c> closes
/// <c>ShiftRepository&lt;DB, Country, CountryRepoDTO, CountryRepoDTO&gt;</c> and nothing else — the repository
/// resolves the automatic maps through the host's mapper and CRUD flows through them.
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
    public async Task Country_InsertAndView_ThroughTheAutomaticMaps()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DB>();
        var countryRepo = scope.ServiceProvider.GetRequiredService<CountryRepository>();

        Assert.IsType<ShiftMapperEntityMapper<Country, CountryRepoDTO, CountryRepoDTO>>(countryRepo.ShiftRepositoryOptions.Mapper);

        var dto = new CountryRepoDTO { Name = "SourceGen Country" };

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
    public async Task Country_Update_ThroughTheAutomaticMaps()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DB>();
        var countryRepo = scope.ServiceProvider.GetRequiredService<CountryRepository>();

        var entity = new Country();
        countryRepo.Add(entity);
        await countryRepo.UpsertAsync(entity, new CountryRepoDTO { Name = "Before Gen Update" }, ActionTypes.Insert, userId: null, idempotencyKey: null, disableDefaultDataLevelAccess: true, disableGlobalFilters: true);
        await countryRepo.SaveChangesAsync();

        db.ChangeTracker.Clear();

        var found = (await countryRepo.FindAsync(entity.ID, asOf: null, disableDefaultDataLevelAccess: true, disableGlobalFilters: true))!;
        await countryRepo.UpsertAsync(found, new CountryRepoDTO { Name = "After Gen Update" }, ActionTypes.Update, userId: null, idempotencyKey: null, disableDefaultDataLevelAccess: true, disableGlobalFilters: true);
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

/// <summary>
/// The CONFIGURED door: a customization written in the repository (<c>o.Mapping(m =&gt; …)</c>) or in an entity's
/// <c>ConfigureRepository</c> is part of the map itself — it applies wherever the map runs, not only inside the
/// repository. When a service maps the pair before any repository was constructed, the mapper constructs the
/// configuring repository from DI on its own (<c>ShiftEntityConfiguratorResolver</c>) and the value is there.
/// </summary>
[Collection("API Collection")]
public class RepositoryConfigurationTests
{
    private readonly CustomWebApplicationFactory factory;

    public RepositoryConfigurationTests(CustomWebApplicationFactory factory) => this.factory = factory;

    // The lines carry their Product: the list projection composes it, and a required navigation is not
    // null-guarded in the projection (a join always matches), so LINQ-to-objects needs it filled.
    private static Invoice InvoiceWithLines() => new()
    {
        ManualReference = "INV-T",
        InvoiceLines = new HashSet<InvoiceLine>
        {
            new InvoiceLine { Description = "A", Price = 2.5m, ProductID = 1, Product = new Product { ID = 1, Name = "P1" } },
            new InvoiceLine { Description = "B", Price = 4m, ProductID = 2, Product = new Product { ID = 2, Name = "P2" } },
        },
    };

    [Fact]
    public void ARepositoryCustomization_AppliesThroughTheRepository()
    {
        using var scope = factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<InvoiceRepository>();

        var row = repo.MapToList(new[] { InvoiceWithLines() }.AsQueryable()).Single();

        Assert.Equal(6.5m, row.Total);   // InvoiceRepository: m.List.ForMember(d => d.Total, ... Sum(l => l.Price))
    }

    [Fact]
    public void ARepositoryCustomization_AppliesToTheSameMapAnywhere_BeforeAnyRepositoryRan()
    {
        // A FRESH scope in which no InvoiceRepository has been constructed: the map is used first by a
        // "service". The customized member's value is pulled by constructing the repository from DI.
        using var scope = factory.Services.CreateScope();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        var row = mapper.ProjectTo<Invoice, InvoiceListDTO>(new[] { InvoiceWithLines() }.AsQueryable()).Single();

        Assert.Equal(6.5m, row.Total);
        Assert.Equal(2, row.InvoiceLines.Count);   // and the children still nest, with nothing configured for them
    }

    [Fact]
    public void AnEntityConfiguration_AppliesToTheSameMapAnywhere_BeforeAnyRepositoryRan()
    {
        // Country.ConfigureRepository (IConfiguresShiftRepository) customizes the api/country-generated list
        // map. The configuring type is the ENTITY, so the pull constructs the built-in repository closed over
        // it — through the host's DbContextOptions — rather than the entity.
        using var scope = factory.Services.CreateScope();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        var row = mapper.ProjectTo<Country, CountryGeneratedDTO>(new[] { new Country { Name = "Alpha" } }.AsQueryable()).Single();

        Assert.Equal("Alpha (via IConfiguresShiftRepository)", row.Name);
        Assert.Equal("0", row.ID);   // the convention binding beside it is untouched
    }

    [Fact]
    public void ATripleWithNoCustomization_HasNothingToPull()
    {
        using var scope = factory.Services.CreateScope();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        var row = mapper.ProjectTo<Country, CountryRepoDTO>(new[] { new Country { Name = "Beta" } }.AsQueryable()).Single();

        Assert.Equal("Beta", row.Name);
    }
}
