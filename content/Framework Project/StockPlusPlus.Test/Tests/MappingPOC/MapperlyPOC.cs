// =============================================================================
// MapperlyPOC.cs — Mapperly source generator (compile-time, zero reflection)
// =============================================================================
//
// APPROACH: Mapperly [Mapper] partial classes with compile-time code generation.
//   - Source generator produces mapping code at build time
//   - Custom user-defined methods for complex transformations
//   - No runtime reflection
//   - Compile-time diagnostics for unmapped properties
//
// | Concern                        | Mapperly                        |
// |--------------------------------|---------------------------------|
// | VehicleVIN implicit flattening | DEFAULT ON (configurable off)   |
// | IQueryable projection          | Limited (simple cases only)     |
// | Compile-time safety            | High (warnings/errors)          |
// | Boilerplate                    | Low                             |
// | Runtime reflection             | None                            |
// | Migration effort from AM       | Medium                          |
// | Missing mapping detection      | Compile-time warnings           |
// =============================================================================

using Riok.Mapperly.Abstractions;

namespace MappingPOC.Mapperly;

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
    public string? VehicleVIN { get; set; }
}

#endregion

#region =================== MAPPERLY MAPPER CLASSES ====================

// ---------------------------------------------------------------
// Product Mapper — Entity ↔ DTO
// ---------------------------------------------------------------
// Mapperly generates the implementation at compile time.
// Custom methods (non-partial) are used by the generator for type conversions.

[Mapper]
public partial class ProductMapper
{
    // --- Entity → ViewDTO ---
    [MapProperty(nameof(Product.ProductCategoryID), nameof(ProductDTO.ProductCategory))]
    [MapProperty(nameof(Product.ProductBrandID), nameof(ProductDTO.ProductBrand))]
    [MapProperty(nameof(Product.CountryOfOriginID), nameof(ProductDTO.CountryOfOrigin))]
    [MapperIgnoreSource(nameof(Product.ProductCategory))]  // Don't flatten navigation
    [MapperIgnoreSource(nameof(Product.ProductBrand))]
    [MapperIgnoreSource(nameof(Product.CountryOfOrigin))]
    [MapperIgnoreSource(nameof(Product.ReloadAfterSave))]
    public partial ProductDTO ToViewDTO(Product entity);

    // --- DTO → Entity (merge into existing) ---
    [MapperIgnoreTarget(nameof(Product.ProductCategory))]
    [MapperIgnoreTarget(nameof(Product.ProductBrand))]
    [MapperIgnoreTarget(nameof(Product.CountryOfOrigin))]
    [MapperIgnoreTarget(nameof(Product.ID))]
    [MapperIgnoreTarget(nameof(Product.CreateDate))]
    [MapperIgnoreTarget(nameof(Product.LastSaveDate))]
    [MapperIgnoreTarget(nameof(Product.ReloadAfterSave))]
    [MapperIgnoreTarget(nameof(Product.IsDeleted))]
    [MapperIgnoreSource(nameof(ProductDTO.ID))]
    [MapperIgnoreSource(nameof(ProductDTO.CreateDate))]
    [MapperIgnoreSource(nameof(ProductDTO.LastSaveDate))]
    [MapperIgnoreSource(nameof(ProductDTO.IsDeleted))]
    [MapProperty(nameof(ProductDTO.ProductCategory), nameof(Product.ProductCategoryID))]
    [MapProperty(nameof(ProductDTO.ProductBrand), nameof(Product.ProductBrandID))]
    [MapProperty(nameof(ProductDTO.CountryOfOrigin), nameof(Product.CountryOfOriginID))]
    public partial void ToEntity(ProductDTO dto, Product existing);

    // --- Custom type conversions (Mapperly discovers these by signature) ---

    // long → ShiftEntitySelectDTO (for entity → DTO direction)
    private ShiftEntitySelectDTO LongToSelectDTO(long id)
        => new() { Value = id.ToString() };

    private ShiftEntitySelectDTO? NullableLongToSelectDTO(long? id)
        => id.HasValue ? new ShiftEntitySelectDTO { Value = id.Value.ToString() } : null;

    // ShiftEntitySelectDTO → long (for DTO → entity direction)
    private long SelectDTOToLong(ShiftEntitySelectDTO dto)
        => long.Parse(dto.Value);

    private long? SelectDTOToNullableLong(ShiftEntitySelectDTO? dto)
        => dto != null && !string.IsNullOrWhiteSpace(dto.Value) ? long.Parse(dto.Value) : null;

    // long → string (for ID mapping)
    private string LongToString(long id) => id.ToString();

    // List<ShiftFileDTO> ↔ string (JSON serialization)
    private List<ShiftFileDTO>? StringToFileList(string? json)
        => string.IsNullOrWhiteSpace(json) ? new List<ShiftFileDTO>() : JsonSerializer.Deserialize<List<ShiftFileDTO>>(json);

    private string? FileListToString(List<ShiftFileDTO>? files)
        => files == null ? null : JsonSerializer.Serialize(files);
}

// ---------------------------------------------------------------
// Product List Mapper — for IQueryable projection
// ---------------------------------------------------------------
// NOTE: Mapperly can generate queryable projections via ProjectTo,
// but complex cases (navigation → string) often need manual Select().

[Mapper]
public partial class ProductListMapper
{
    [MapperIgnoreSource(nameof(Product.Description))]
    [MapperIgnoreSource(nameof(Product.Photos))]
    [MapperIgnoreSource(nameof(Product.ProductCategoryID))]
    [MapperIgnoreSource(nameof(Product.ProductBrandID))]
    [MapperIgnoreSource(nameof(Product.CountryOfOriginID))]
    [MapperIgnoreSource(nameof(Product.CountryOfOrigin))]
    [MapperIgnoreSource(nameof(Product.ReloadAfterSave))]
    [MapperIgnoreSource(nameof(Product.CreateDate))]
    [MapperIgnoreSource(nameof(Product.LastSaveDate))]
    [MapProperty(nameof(Product.ProductBrand) + "." + nameof(ProductBrand.Name), nameof(ProductListDTO.ProductBrand))]
    [MapProperty(nameof(Product.ProductCategory) + "." + nameof(ProductCategory.Name), nameof(ProductListDTO.Category))]
    private partial ProductListDTO ToListDTO(Product entity);

    private string LongToString(long id) => id.ToString();

    // IQueryable projection — Mapperly can generate this
    public IQueryable<ProductListDTO> ProjectToList(IQueryable<Product> query)
        => query.Select(p => new ProductListDTO
        {
            ID = p.ID.ToString(),
            Name = p.Name,
            Price = p.Price,
            IsDeleted = p.IsDeleted,
            ProductBrand = p.ProductBrand != null ? p.ProductBrand.Name : null,
            Category = p.ProductCategory != null ? p.ProductCategory.Name : null,
        });
}

// ---------------------------------------------------------------
// CompanyBranch Mapper — collection + CustomFields
// ---------------------------------------------------------------

[Mapper]
public partial class CompanyBranchMapper
{
    [MapperIgnoreSource(nameof(CompanyBranch.CompanyBranchServices))]
    [MapperIgnoreSource(nameof(CompanyBranch.ReloadAfterSave))]
    [MapperIgnoreTarget(nameof(CompanyBranchDTO.Services))]
    private partial CompanyBranchDTO BaseToViewDTO(CompanyBranch entity);

    public CompanyBranchDTO ToViewDTO(CompanyBranch entity)
    {
        var dto = BaseToViewDTO(entity);

        // Collection mapping: junction table → List<SelectDTO>
        dto.Services = entity.CompanyBranchServices
            .Select(s => new ShiftEntitySelectDTO
            {
                Value = s.ServiceID.ToString(),
                Text = s.Service?.Name
            })
            .ToList();

        return dto;
    }

    public CompanyBranch ToEntity(CompanyBranchDTO dto, CompanyBranch existing)
    {
        existing.Name = dto.Name;

        // CustomFields with password-skip logic
        foreach (var key in dto.CustomFields.Keys)
        {
            var srcField = dto.CustomFields[key];
            if (srcField.IsPassword && srcField.Value == null)
                continue;

            existing.CustomFields[key] = new CustomField
            {
                Value = srcField.Value,
                DisplayName = srcField.DisplayName,
                IsPassword = srcField.IsPassword,
                IsEncrypted = srcField.IsEncrypted,
            };
        }

        // Collection sync
        existing.CompanyBranchServices = dto.Services
            .Select(s => new CompanyBranchService
            {
                CompanyBranchID = existing.ID,
                ServiceID = long.Parse(s.Value),
            })
            .ToList();

        return existing;
    }

    private string LongToString(long id) => id.ToString();
}

// ---------------------------------------------------------------
// VehicleRepair Mapper — demonstrates flattening behavior
// ---------------------------------------------------------------

// DEFAULT: Mapperly WILL flatten Vehicle.VIN → VehicleVIN
[Mapper]
public partial class VehicleRepairMapperUnsafe
{
    [MapProperty(nameof(VehicleRepair.VehicleID), nameof(VehicleRepairDTO.Vehicle))]
    [MapperIgnoreSource(nameof(VehicleRepair.Vehicle))]
    [MapperIgnoreSource(nameof(VehicleRepair.ReloadAfterSave))]
    [MapperIgnoreSource(nameof(VehicleRepair.CreateDate))]
    [MapperIgnoreSource(nameof(VehicleRepair.LastSaveDate))]
    public partial VehicleRepairDTO ToViewDTO(VehicleRepair entity);

    private ShiftEntitySelectDTO LongToSelectDTO(long id) => new() { Value = id.ToString() };
    private string LongToString(long id) => id.ToString();
}

// SAFE: Explicitly ignore VehicleVIN to prevent flattening
[Mapper]
public partial class VehicleRepairMapperSafe
{
    [MapProperty(nameof(VehicleRepair.VehicleID), nameof(VehicleRepairDTO.Vehicle))]
    [MapperIgnoreSource(nameof(VehicleRepair.Vehicle))]
    [MapperIgnoreSource(nameof(VehicleRepair.ReloadAfterSave))]
    [MapperIgnoreSource(nameof(VehicleRepair.CreateDate))]
    [MapperIgnoreSource(nameof(VehicleRepair.LastSaveDate))]
    [MapperIgnoreTarget(nameof(VehicleRepairDTO.VehicleVIN))]
    public partial VehicleRepairDTO ToViewDTO(VehicleRepair entity);

    private ShiftEntitySelectDTO LongToSelectDTO(long id) => new() { Value = id.ToString() };
    private string LongToString(long id) => id.ToString();
}

// ---------------------------------------------------------------
// Entity-to-Entity copy (ReloadAfterSave)
// ---------------------------------------------------------------

[Mapper]
public partial class ProductEntityCopier
{
    [MapperIgnoreTarget(nameof(Product.ReloadAfterSave))]
    [MapperIgnoreTarget(nameof(Product.ID))]
    public partial void CopyEntity(Product source, Product target);
}

#endregion

#region =================== REPOSITORY INTEGRATION SKETCH ==============

// With Mapperly, each mapper is a concrete class (source-generated).
// The repository would hold a reference to the mapper instance.

public class RepositorySketch<TEntity, TListDTO, TViewDTO>
    where TEntity : ShiftEntityBase
    where TListDTO : ShiftEntityListDTO
    where TViewDTO : ShiftEntityViewAndUpsertDTO
{
    // Unlike AutoMapper's single IMapper, you'd need typed mapper references.
    // Auto-registration via assembly scanning for mapper classes is possible
    // but less elegant than AutoMapper's profile scanning.

    // Example usage (not a real working pattern, just illustration):
    // private readonly ProductMapper _productMapper;
    // private readonly ProductListMapper _listMapper;
    // private readonly ProductEntityCopier _copier;
}

#endregion

#region ======================== TESTS =================================

public class MapperlyMappingTests
{
    // ---------------------------------------------------------------
    // Test 1: Simple Entity → DTO
    // ---------------------------------------------------------------
    [Fact]
    public void SimpleEntityToDto()
    {
        var entity = CreateSampleProduct();
        var mapper = new ProductMapper();

        var dto = mapper.ToViewDTO(entity);

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
        var mapper = new ProductMapper();

        var dto = mapper.ToViewDTO(entity);

        // FK → SelectDTO (via custom LongToSelectDTO method)
        Assert.Equal("10", dto.ProductCategory.Value);
        Assert.Equal("20", dto.ProductBrand.Value);
        Assert.NotNull(dto.CountryOfOrigin);
        Assert.Equal("30", dto.CountryOfOrigin!.Value);

        // NOTE: Text is NOT populated because we mapped from the FK (long),
        // not from the navigation property. To get Text, you'd need a
        // post-mapping step or a different mapping strategy.
        Assert.Null(dto.ProductCategory.Text);
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

        var mapper = new ProductListMapper();
        var result = mapper.ProjectToList(products).ToList();

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
        var mapper = new ProductMapper();
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

        mapper.ToEntity(dto, existing);

        Assert.Equal(42, existing.ID); // ID preserved
        Assert.Equal("Updated Widget", existing.Name);
        Assert.Equal(24.99m, existing.Price);
        Assert.Equal(10, existing.ProductCategoryID);
        Assert.Equal(20, existing.ProductBrandID);
        Assert.Equal(30, existing.CountryOfOriginID);

        // Navigation properties are NOT touched
        Assert.Null(existing.ProductCategory);
        Assert.Null(existing.ProductBrand);
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

        var mapper = new CompanyBranchMapper();
        var dto = mapper.ToViewDTO(entity);

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
                ["apiKey"] = new CustomField { Value = null, DisplayName = "API Key", IsPassword = true },
                ["label"] = new CustomField { Value = "New Label", DisplayName = "Label" },
            }
        };

        var mapper = new CompanyBranchMapper();
        mapper.ToEntity(dto, existing);

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
        var mapper = new ProductMapper();

        var dto = mapper.ToViewDTO(entity);

        Assert.NotNull(dto.Photos);
        Assert.Single(dto.Photos!);
        Assert.Equal("front.jpg", dto.Photos![0].Name);

        // Round-trip: DTO → Entity
        var existing = new Product { ID = 42 };
        mapper.ToEntity(dto, existing);

        Assert.NotNull(existing.Photos);
        var parsed = JsonSerializer.Deserialize<List<ShiftFileDTO>>(existing.Photos!);
        Assert.Single(parsed!);
        Assert.Equal("front.jpg", parsed![0].Name);
    }

    // ---------------------------------------------------------------
    // Test 8: VehicleVIN — Mapperly flattening behavior
    // ---------------------------------------------------------------
    [Fact]
    public void VehicleVIN_MapperlyFlatteningBehavior()
    {
        var entity = new VehicleRepair
        {
            ID = 1,
            RepairDescription = "Brake pad replacement",
            VehicleID = 50,
            Vehicle = new Vehicle { ID = 50, VIN = "1HGBH41JXMN109186", Make = "Honda" }
        };

        // SAFE mapper (with [MapperIgnoreTarget] on VehicleVIN):
        var safeMapper = new VehicleRepairMapperSafe();
        var safeDto = safeMapper.ToViewDTO(entity);

        Assert.Equal("50", safeDto.Vehicle.Value);
        Assert.Null(safeDto.VehicleVIN); // Correctly ignored

        // UNSAFE mapper (without the ignore):
        // Mapperly MAY flatten Vehicle.VIN → VehicleVIN depending on
        // how it resolves the mapping. The key insight is that Mapperly,
        // like AutoMapper, uses naming conventions to discover mappings.
        // If it can't resolve the flattening (e.g., Vehicle is already
        // mapped to a SelectDTO), it may produce a compile warning instead.
        // The behavior depends on the specific Mapperly version.
        var unsafeMapper = new VehicleRepairMapperUnsafe();
        var unsafeDto = unsafeMapper.ToViewDTO(entity);
        Assert.Equal("50", unsafeDto.Vehicle.Value);
        // VehicleVIN may or may not be populated depending on Mapperly's
        // resolution strategy. The point is: with explicit ignores, you're safe.
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

        var copier = new ProductEntityCopier();
        copier.CopyEntity(fresh, tracked);

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
