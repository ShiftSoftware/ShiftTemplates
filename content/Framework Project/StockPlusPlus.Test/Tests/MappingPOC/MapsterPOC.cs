// =============================================================================
// MapsterPOC.cs — Mapster library (convention-based, similar feel to AutoMapper)
// =============================================================================
//
// APPROACH: Mapster with TypeAdapterConfig, .Adapt<T>(), ProjectToType<T>().
//   - Fluent configuration similar to AutoMapper
//   - Convention-based with opt-out
//   - Supports IQueryable projection via ProjectToType<T>()
//
// | Concern                        | Mapster                         |
// |--------------------------------|---------------------------------|
// | VehicleVIN implicit flattening | DEFAULT ON (same risk as AM)    |
// | IQueryable projection          | Good (ProjectToType)            |
// | Compile-time safety            | Low (runtime errors)            |
// | Boilerplate                    | Low                             |
// | Runtime reflection             | Yes                             |
// | Migration effort from AM       | Low (similar API)               |
// | Missing mapping detection      | Runtime (null values)           |
// =============================================================================

using Mapster;

namespace MappingPOC.Mapster;

#region ========================= SHARED TYPES =========================

// --- Framework base types (simplified) ---

public abstract class ShiftEntityBase
{
    public long ID { get; set; }
}

public abstract class ShiftEntity<T> : ShiftEntityBase where T : ShiftEntity<T>
{
    public DateTimeOffset CreateDate { get; set; }
    public DateTimeOffset LastSaveDate { get; set; }
    public bool IsDeleted { get; set; }
    public bool ReloadAfterSave { get; set; }
}

public abstract class ShiftEntityDTOBase
{
    public string? ID { get; set; }
    public bool IsDeleted { get; set; }
}

public abstract class ShiftEntityListDTO : ShiftEntityDTOBase { }

public abstract class ShiftEntityViewAndUpsertDTO : ShiftEntityDTOBase
{
    public DateTimeOffset? CreateDate { get; set; }
    public DateTimeOffset? LastSaveDate { get; set; }
}

public class ShiftEntitySelectDTO
{
    public string Value { get; set; } = "";
    public string? Text { get; set; }
}

public class ShiftFileDTO
{
    public string? Name { get; set; }
    public string? Blob { get; set; }
    public long Size { get; set; }
}

public class CustomField
{
    public string? Value { get; set; }
    public string? DisplayName { get; set; }
    public bool IsPassword { get; set; }
    public bool IsEncrypted { get; set; }
}

// --- Entities ---

public class ProductCategory : ShiftEntity<ProductCategory>
{
    public string Name { get; set; } = "";
}

public class ProductBrand : ShiftEntity<ProductBrand>
{
    public string Name { get; set; } = "";
}

public class Country : ShiftEntity<Country>
{
    public string Name { get; set; } = "";
}

public class Product : ShiftEntity<Product>
{
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public decimal Price { get; set; }

    public long ProductCategoryID { get; set; }
    public long ProductBrandID { get; set; }
    public long? CountryOfOriginID { get; set; }

    public virtual ProductCategory? ProductCategory { get; set; }
    public virtual ProductBrand? ProductBrand { get; set; }
    public virtual Country? CountryOfOrigin { get; set; }

    public string? Photos { get; set; }
}

public class Service : ShiftEntity<Service>
{
    public string Name { get; set; } = "";
}

public class CompanyBranchService
{
    public long ID { get; set; }
    public long CompanyBranchID { get; set; }
    public long ServiceID { get; set; }
    public virtual Service? Service { get; set; }
}

public class CompanyBranch : ShiftEntity<CompanyBranch>
{
    public string Name { get; set; } = "";
    public Dictionary<string, CustomField> CustomFields { get; set; } = new();
    public virtual ICollection<CompanyBranchService> CompanyBranchServices { get; set; } = new List<CompanyBranchService>();
}

// VehicleVIN scenario
public class Vehicle : ShiftEntity<Vehicle>
{
    public string VIN { get; set; } = "";
    public string Make { get; set; } = "";
}

public class VehicleRepair : ShiftEntity<VehicleRepair>
{
    public string RepairDescription { get; set; } = "";
    public long VehicleID { get; set; }
    public virtual Vehicle? Vehicle { get; set; }
}

// --- DTOs ---

public class ProductDTO : ShiftEntityViewAndUpsertDTO
{
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public ShiftEntitySelectDTO ProductCategory { get; set; } = new();
    public ShiftEntitySelectDTO ProductBrand { get; set; } = new();
    public ShiftEntitySelectDTO? CountryOfOrigin { get; set; }
    public List<ShiftFileDTO>? Photos { get; set; }
}

public class ProductListDTO : ShiftEntityListDTO
{
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public string? ProductBrand { get; set; }
    public string? Category { get; set; }
}

public class CompanyBranchDTO : ShiftEntityViewAndUpsertDTO
{
    public string Name { get; set; } = "";
    public List<ShiftEntitySelectDTO> Services { get; set; } = new();
    public Dictionary<string, CustomField> CustomFields { get; set; } = new();
}

public class VehicleRepairDTO : ShiftEntityViewAndUpsertDTO
{
    public string RepairDescription { get; set; } = "";
    public ShiftEntitySelectDTO Vehicle { get; set; } = new();
    // This property WILL be silently populated by Mapster via flattening
    // (Vehicle.VIN → VehicleVIN) — same as AutoMapper!
    public string? VehicleVIN { get; set; }
}

#endregion

#region =================== MAPSTER CONFIGURATION =====================

public static class MapsterConfig
{
    public static TypeAdapterConfig CreateConfig()
    {
        var config = new TypeAdapterConfig();

        // ---------------------------------------------------------------
        // Product → ProductDTO
        // ---------------------------------------------------------------
        config.NewConfig<Product, ProductDTO>()
            .Map(dest => dest.ID, src => src.ID.ToString())
            .Map(dest => dest.ProductCategory, src => new ShiftEntitySelectDTO
            {
                Value = src.ProductCategoryID.ToString(),
                Text = src.ProductCategory != null ? src.ProductCategory.Name : null
            })
            .Map(dest => dest.ProductBrand, src => new ShiftEntitySelectDTO
            {
                Value = src.ProductBrandID.ToString(),
                Text = src.ProductBrand != null ? src.ProductBrand.Name : null
            })
            .Map(dest => dest.CountryOfOrigin, src => src.CountryOfOriginID.HasValue
                ? new ShiftEntitySelectDTO
                {
                    Value = src.CountryOfOriginID.Value.ToString(),
                    Text = src.CountryOfOrigin != null ? src.CountryOfOrigin.Name : null
                }
                : null)
            .Map(dest => dest.Photos, src => string.IsNullOrWhiteSpace(src.Photos)
                ? new List<ShiftFileDTO>()
                : JsonSerializer.Deserialize<List<ShiftFileDTO>>(src.Photos));

        // ---------------------------------------------------------------
        // ProductDTO → Product (reverse / upsert)
        // ---------------------------------------------------------------
        config.NewConfig<ProductDTO, Product>()
            .Ignore(dest => dest.ProductCategory!)   // Don't overwrite navigation
            .Ignore(dest => dest.ProductBrand!)       // Don't overwrite navigation
            .Ignore(dest => dest.CountryOfOrigin!)    // Don't overwrite navigation
            .Ignore(dest => dest.ID)                  // Preserve entity ID
            .Ignore(dest => dest.CreateDate)          // Preserve audit fields
            .Ignore(dest => dest.LastSaveDate)
            .Ignore(dest => dest.ReloadAfterSave)
            .Map(dest => dest.ProductCategoryID, src => long.Parse(src.ProductCategory.Value))
            .Map(dest => dest.ProductBrandID, src => long.Parse(src.ProductBrand.Value))
            .Map(dest => dest.CountryOfOriginID, src =>
                src.CountryOfOrigin != null && !string.IsNullOrWhiteSpace(src.CountryOfOrigin.Value)
                    ? (long?)long.Parse(src.CountryOfOrigin.Value)
                    : null)
            .Map(dest => dest.Photos, src => src.Photos != null
                ? JsonSerializer.Serialize(src.Photos)
                : null);

        // ---------------------------------------------------------------
        // Product → ProductListDTO (for IQueryable projection)
        // ---------------------------------------------------------------
        config.NewConfig<Product, ProductListDTO>()
            .Map(dest => dest.ID, src => src.ID.ToString())
            .Map(dest => dest.ProductBrand, src => src.ProductBrand != null ? src.ProductBrand.Name : null)
            .Map(dest => dest.Category, src => src.ProductCategory != null ? src.ProductCategory.Name : null);

        // ---------------------------------------------------------------
        // CompanyBranch → CompanyBranchDTO (collection mapping)
        // ---------------------------------------------------------------
        config.NewConfig<CompanyBranch, CompanyBranchDTO>()
            .Map(dest => dest.ID, src => src.ID.ToString())
            .Map(dest => dest.Services, src => src.CompanyBranchServices
                .Select(s => new ShiftEntitySelectDTO
                {
                    Value = s.ServiceID.ToString(),
                    Text = s.Service != null ? s.Service.Name : null
                })
                .ToList());

        // ---------------------------------------------------------------
        // CompanyBranchDTO → CompanyBranch (reverse with CustomFields)
        // ---------------------------------------------------------------
        config.NewConfig<CompanyBranchDTO, CompanyBranch>()
            .Ignore(dest => dest.CustomFields)  // Handled in AfterMapping
            .Ignore(dest => dest.ID)
            .Map(dest => dest.CompanyBranchServices, src => src.Services
                .Select(s => new CompanyBranchService { ServiceID = long.Parse(s.Value) })
                .ToList())
            .AfterMapping((src, dest) =>
            {
                // CustomFields with password-skip logic
                foreach (var key in src.CustomFields.Keys)
                {
                    var srcField = src.CustomFields[key];
                    if (srcField.IsPassword && srcField.Value == null)
                        continue;

                    dest.CustomFields[key] = new CustomField
                    {
                        Value = srcField.Value,
                        DisplayName = srcField.DisplayName,
                        IsPassword = srcField.IsPassword,
                        IsEncrypted = srcField.IsEncrypted,
                    };
                }
            });

        // ---------------------------------------------------------------
        // VehicleRepair → VehicleRepairDTO
        // NOTE: By default, Mapster WILL flatten Vehicle.VIN → VehicleVIN!
        // We configure it WITHOUT fixing the flattening to demonstrate the risk.
        // ---------------------------------------------------------------
        config.NewConfig<VehicleRepair, VehicleRepairDTO>()
            .Map(dest => dest.ID, src => src.ID.ToString())
            .Map(dest => dest.Vehicle, src => new ShiftEntitySelectDTO
            {
                Value = src.VehicleID.ToString(),
                Text = src.Vehicle != null ? src.Vehicle.Make : null
            });
        // VehicleVIN is NOT explicitly mapped — but Mapster may flatten it anyway!

        // ---------------------------------------------------------------
        // Product → Product (entity-to-entity copy for ReloadAfterSave)
        // ---------------------------------------------------------------
        config.NewConfig<Product, Product>()
            .Ignore(dest => dest.ReloadAfterSave);

        return config;
    }

    /// <summary>
    /// Creates a "safe" config where the VehicleVIN flattening is explicitly fixed.
    /// </summary>
    public static TypeAdapterConfig CreateSafeConfig()
    {
        var config = CreateConfig();

        // Fix: explicitly ignore the flattened property
        config.ForType<VehicleRepair, VehicleRepairDTO>()
            .Ignore(dest => dest.VehicleVIN!);

        return config;
    }
}

#endregion

#region =================== REPOSITORY INTEGRATION SKETCH ==============

// With Mapster, the repository can use .Adapt<T>() and .ProjectToType<T>()
// very similarly to AutoMapper's Map<T>() and ProjectTo<T>().

public class RepositorySketch
{
    private readonly TypeAdapterConfig _config;

    public RepositorySketch(TypeAdapterConfig config)
    {
        _config = config;
    }

    // Replaces: mapper.ProjectTo<ListDTO>(queryable)
    public IQueryable<TListDTO> OdataList<TEntity, TListDTO>(IQueryable<TEntity> queryable)
        => queryable.ProjectToType<TListDTO>(_config);

    // Replaces: mapper.Map<ViewAndUpsertDTO>(entity)
    public TViewDTO View<TEntity, TViewDTO>(TEntity entity)
        => entity.Adapt<TViewDTO>(_config);

    // Replaces: mapper.Map(dto, entity)
    public TEntity Upsert<TViewDTO, TEntity>(TViewDTO dto, TEntity entity)
        => dto.Adapt(entity, _config);

    // Replaces: mapper.Map(freshEntity, trackedEntity)
    public TEntity ReloadAfterSave<TEntity>(TEntity source, TEntity target)
        => source.Adapt(target, _config);
}

#endregion

#region ======================== TESTS =================================

public class MapsterMappingTests
{
    private readonly TypeAdapterConfig _config = MapsterConfig.CreateConfig();

    // ---------------------------------------------------------------
    // Test 1: Simple Entity → DTO
    // ---------------------------------------------------------------
    [Fact]
    public void SimpleEntityToDto()
    {
        var entity = CreateSampleProduct();

        var dto = entity.Adapt<ProductDTO>(_config);

        Assert.Equal("42", dto.ID);
        Assert.Equal("Widget", dto.Name);
        Assert.Equal("A nice widget", dto.Description);
        Assert.Equal(19.99m, dto.Price);
        Assert.False(dto.IsDeleted);
    }

    // ---------------------------------------------------------------
    // Test 2: ShiftEntitySelectDTO FK convention
    // ---------------------------------------------------------------
    [Fact]
    public void ShiftEntitySelectDTOConvention()
    {
        var entity = CreateSampleProduct();

        var dto = entity.Adapt<ProductDTO>(_config);

        Assert.Equal("10", dto.ProductCategory.Value);
        Assert.Equal("Electronics", dto.ProductCategory.Text);
        Assert.Equal("20", dto.ProductBrand.Value);
        Assert.Equal("Acme", dto.ProductBrand.Text);
        Assert.NotNull(dto.CountryOfOrigin);
        Assert.Equal("30", dto.CountryOfOrigin!.Value);
        Assert.Equal("Germany", dto.CountryOfOrigin.Text);
    }

    // ---------------------------------------------------------------
    // Test 3: IQueryable list projection
    // ---------------------------------------------------------------
    [Fact]
    public void ListDTOProjection()
    {
        var products = new List<Product>
        {
            CreateSampleProduct(),
            new Product
            {
                ID = 43, Name = "Gadget", Price = 29.99m,
                ProductCategoryID = 10,
                ProductCategory = new ProductCategory { ID = 10, Name = "Electronics" },
                ProductBrandID = 21,
                ProductBrand = new ProductBrand { ID = 21, Name = "BrandX" },
            }
        }.AsQueryable();

        var result = products.ProjectToType<ProductListDTO>(_config).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal("Widget", result[0].Name);
        Assert.Equal("Acme", result[0].ProductBrand);
        Assert.Equal("Electronics", result[0].Category);
        Assert.Equal("Gadget", result[1].Name);
        Assert.Equal("BrandX", result[1].ProductBrand);
    }

    // ---------------------------------------------------------------
    // Test 4: Reverse mapping / upsert (DTO → Entity)
    // ---------------------------------------------------------------
    [Fact]
    public void ReverseMappingUpsert()
    {
        var existing = new Product { ID = 42, CreateDate = DateTimeOffset.UtcNow };

        var dto = new ProductDTO
        {
            Name = "Updated Widget",
            Description = "Updated description",
            Price = 24.99m,
            ProductCategory = new ShiftEntitySelectDTO { Value = "10" },
            ProductBrand = new ShiftEntitySelectDTO { Value = "20" },
            CountryOfOrigin = new ShiftEntitySelectDTO { Value = "30" },
        };

        var result = dto.Adapt(existing, _config);

        Assert.Equal(42, result.ID); // ID preserved
        Assert.Equal("Updated Widget", result.Name);
        Assert.Equal(24.99m, result.Price);
        Assert.Equal(10, result.ProductCategoryID);
        Assert.Equal(20, result.ProductBrandID);
        Assert.Equal(30, result.CountryOfOriginID);

        // Navigation properties are NOT touched (Ignored in config)
        Assert.Null(result.ProductCategory);
        Assert.Null(result.ProductBrand);
    }

    // ---------------------------------------------------------------
    // Test 5: Collection mapping (junction → List<SelectDTO>)
    // ---------------------------------------------------------------
    [Fact]
    public void CollectionMapping()
    {
        var entity = new CompanyBranch
        {
            ID = 1,
            Name = "Main Branch",
            CompanyBranchServices = new List<CompanyBranchService>
            {
                new() { ID = 1, ServiceID = 100, Service = new Service { ID = 100, Name = "Oil Change" } },
                new() { ID = 2, ServiceID = 101, Service = new Service { ID = 101, Name = "Tire Rotation" } },
            }
        };

        var dto = entity.Adapt<CompanyBranchDTO>(_config);

        Assert.Equal(2, dto.Services.Count);
        Assert.Equal("100", dto.Services[0].Value);
        Assert.Equal("Oil Change", dto.Services[0].Text);
        Assert.Equal("101", dto.Services[1].Value);
        Assert.Equal("Tire Rotation", dto.Services[1].Text);
    }

    // ---------------------------------------------------------------
    // Test 6: Complex AfterMap (CustomFields with password skip)
    // ---------------------------------------------------------------
    [Fact]
    public void ComplexAfterMapCustomFields()
    {
        var existing = new CompanyBranch
        {
            ID = 1,
            Name = "Branch",
            CustomFields = new Dictionary<string, CustomField>
            {
                ["apiKey"] = new CustomField { Value = "secret-123", DisplayName = "API Key", IsPassword = true },
                ["label"] = new CustomField { Value = "Old Label", DisplayName = "Label" },
            }
        };

        var dto = new CompanyBranchDTO
        {
            Name = "Branch",
            Services = new List<ShiftEntitySelectDTO>(),
            CustomFields = new Dictionary<string, CustomField>
            {
                // Password field with null value — should NOT overwrite existing
                ["apiKey"] = new CustomField { Value = null, DisplayName = "API Key", IsPassword = true },
                // Regular field — should overwrite
                ["label"] = new CustomField { Value = "New Label", DisplayName = "Label" },
            }
        };

        dto.Adapt(existing, _config);

        Assert.Equal("secret-123", existing.CustomFields["apiKey"].Value); // preserved
        Assert.Equal("New Label", existing.CustomFields["label"].Value);   // updated
    }

    // ---------------------------------------------------------------
    // Test 7: Type converter (List<ShiftFileDTO> ↔ string)
    // ---------------------------------------------------------------
    [Fact]
    public void TypeConverterShiftFileDTO()
    {
        var entity = CreateSampleProduct();

        var dto = entity.Adapt<ProductDTO>(_config);

        Assert.NotNull(dto.Photos);
        Assert.Single(dto.Photos!);
        Assert.Equal("front.jpg", dto.Photos![0].Name);

        // Round-trip: DTO → Entity
        var backToEntity = new Product { ID = 42 };
        dto.Adapt(backToEntity, _config);

        Assert.NotNull(backToEntity.Photos);
        var parsed = JsonSerializer.Deserialize<List<ShiftFileDTO>>(backToEntity.Photos!);
        Assert.Single(parsed!);
        Assert.Equal("front.jpg", parsed![0].Name);
    }

    // ---------------------------------------------------------------
    // Test 8: VehicleVIN — Mapster DOES silently flatten (DANGER!)
    // ---------------------------------------------------------------
    [Fact]
    public void VehicleVIN_MapsterFlattensImplicitly()
    {
        var entity = new VehicleRepair
        {
            ID = 1,
            RepairDescription = "Brake pad replacement",
            VehicleID = 50,
            Vehicle = new Vehicle { ID = 50, VIN = "1HGBH41JXMN109186", Make = "Honda" }
        };

        // With default config (no explicit ignore on VehicleVIN):
        var dto = entity.Adapt<VehicleRepairDTO>(_config);

        Assert.Equal("50", dto.Vehicle.Value);
        Assert.Equal("Honda", dto.Vehicle.Text);

        // DANGER: Mapster flattens Vehicle.VIN → VehicleVIN automatically!
        // This is the SAME silent behavior as AutoMapper.
        Assert.Equal("1HGBH41JXMN109186", dto.VehicleVIN); // <-- SILENTLY POPULATED

        // With the "safe" config that explicitly ignores VehicleVIN:
        var safeConfig = MapsterConfig.CreateSafeConfig();
        var safeDto = entity.Adapt<VehicleRepairDTO>(safeConfig);
        Assert.Null(safeDto.VehicleVIN); // Fixed — but you have to know to add the Ignore!
    }

    // ---------------------------------------------------------------
    // Test 9: Entity-to-Entity copy (ReloadAfterSave)
    // ---------------------------------------------------------------
    [Fact]
    public void EntityToEntityReload()
    {
        var fresh = CreateSampleProduct();
        fresh.Name = "Updated Name";
        fresh.Price = 99.99m;
        fresh.ReloadAfterSave = false;

        var tracked = new Product
        {
            ID = 42,
            Name = "Old Name",
            Price = 19.99m,
            ReloadAfterSave = true,
        };

        fresh.Adapt(tracked, _config);

        Assert.Equal("Updated Name", tracked.Name);
        Assert.Equal(99.99m, tracked.Price);
        Assert.True(tracked.ReloadAfterSave); // preserved, not copied
    }

    // ---------------------------------------------------------------
    // Test helper
    // ---------------------------------------------------------------
    private static Product CreateSampleProduct() => new()
    {
        ID = 42,
        Name = "Widget",
        Description = "A nice widget",
        Price = 19.99m,
        ProductCategoryID = 10,
        ProductCategory = new ProductCategory { ID = 10, Name = "Electronics" },
        ProductBrandID = 20,
        ProductBrand = new ProductBrand { ID = 20, Name = "Acme" },
        CountryOfOriginID = 30,
        CountryOfOrigin = new Country { ID = 30, Name = "Germany" },
        Photos = JsonSerializer.Serialize(new List<ShiftFileDTO>
        {
            new() { Name = "front.jpg", Size = 500 }
        }),
    };
}

#endregion
