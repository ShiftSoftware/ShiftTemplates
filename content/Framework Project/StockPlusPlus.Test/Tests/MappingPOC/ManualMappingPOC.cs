// =============================================================================
// ManualMappingPOC.cs — Hand-written mapping (no external library)
// =============================================================================
//
// APPROACH: Explicit mapping methods per entity/DTO pair.
//   - Each mapper implements IShiftEntityMapper<TEntity, TListDTO, TViewDTO>
//   - IQueryable projection via explicit Select() expressions
//   - ShiftEntitySelectDTO FK convention handled by opt-in helper (reflection)
//     OR fully explicit code (developer's choice)
//
// | Concern                        | Manual Mapping                  |
// |--------------------------------|---------------------------------|
// | VehicleVIN implicit flattening | IMPOSSIBLE — no magic           |
// | IQueryable projection          | Full control (Select)           |
// | Compile-time safety            | Medium (no codegen)             |
// | Boilerplate                    | High                            |
// | Runtime reflection             | Minimal (FK helper only)        |
// | Migration effort from AM       | High                            |
// | Missing mapping detection      | Runtime (null values)           |
// =============================================================================

namespace MappingPOC.Manual;

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

    // FKs
    public long ProductCategoryID { get; set; }
    public long ProductBrandID { get; set; }
    public long? CountryOfOriginID { get; set; }

    // Navigation properties
    public virtual ProductCategory? ProductCategory { get; set; }
    public virtual ProductBrand? ProductBrand { get; set; }
    public virtual Country? CountryOfOrigin { get; set; }

    // JSON-serialized file list
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
    // NOTE: No "VehicleVIN" property here — but even if someone added one,
    // manual mapping would never silently populate it from Vehicle.VIN.
    // With AutoMapper, adding this property WOULD silently flatten Vehicle.VIN into it.
    public string? VehicleVIN { get; set; }
}

#endregion

#region =================== MAPPING INFRASTRUCTURE ====================

// --- Mapper interface (replaces IMapper in ShiftRepository) ---

public interface IShiftEntityMapper<TEntity, TListDTO, TViewDTO>
    where TEntity : ShiftEntityBase
    where TListDTO : ShiftEntityListDTO
    where TViewDTO : ShiftEntityViewAndUpsertDTO
{
    TViewDTO ToViewDTO(TEntity entity);
    TEntity ToEntity(TViewDTO dto, TEntity existing);
    IQueryable<TListDTO> ProjectToList(IQueryable<TEntity> query);
    void CopyEntity(TEntity source, TEntity target);
}

// --- ShiftEntitySelectDTO helper (reflection-based convention, same logic as current AutoMapperExtensions) ---

public static class ShiftEntitySelectDTOHelper
{
    /// <summary>
    /// Entity → DTO: For each ShiftEntitySelectDTO property on the DTO,
    /// find {PropertyName}ID on the entity and populate SelectDTO.Value.
    /// </summary>
    public static void MapFKsToSelectDTOs(object entity, object dto)
    {
        foreach (var property in dto.GetType().GetProperties())
        {
            if (property.PropertyType != typeof(ShiftEntitySelectDTO))
                continue;

            var selectDTO = (ShiftEntitySelectDTO?)property.GetValue(dto);
            if (selectDTO is not null && (!string.IsNullOrWhiteSpace(selectDTO.Value) || selectDTO.Text is not null))
                continue; // already mapped explicitly

            var fkName = $"{property.Name}ID";
            var fkProp = entity.GetType().GetProperties()
                .FirstOrDefault(x => x.Name.Equals(fkName, StringComparison.InvariantCultureIgnoreCase));

            if (fkProp is not null)
            {
                var value = fkProp.GetValue(entity)?.ToString() ?? "";
                property.SetValue(dto, new ShiftEntitySelectDTO { Value = value });
            }
        }
    }

    /// <summary>
    /// DTO → Entity: For each ShiftEntitySelectDTO property on the DTO,
    /// parse Value back into {PropertyName}ID on the entity.
    /// </summary>
    public static void MapSelectDTOsToFKs(object dto, object entity)
    {
        foreach (var property in dto.GetType().GetProperties())
        {
            if (property.PropertyType != typeof(ShiftEntitySelectDTO))
                continue;

            var selectDTO = (ShiftEntitySelectDTO?)property.GetValue(dto);
            if (selectDTO is null || string.IsNullOrWhiteSpace(selectDTO.Value))
                continue;

            var fkName = $"{property.Name}ID";
            var fkProp = entity.GetType().GetProperties()
                .FirstOrDefault(x => x.Name.Equals(fkName, StringComparison.InvariantCultureIgnoreCase));

            if (fkProp is not null)
            {
                if (fkProp.PropertyType == typeof(long))
                    fkProp.SetValue(entity, long.Parse(selectDTO.Value));
                else if (fkProp.PropertyType == typeof(long?))
                    fkProp.SetValue(entity, (long?)long.Parse(selectDTO.Value));
            }
        }
    }
}

// --- Type converter helpers ---

public static class ShiftFileConverter
{
    public static string? Serialize(List<ShiftFileDTO>? files)
        => files == null ? null : JsonSerializer.Serialize(files);

    public static List<ShiftFileDTO>? Deserialize(string? json)
        => string.IsNullOrWhiteSpace(json) ? new List<ShiftFileDTO>() : JsonSerializer.Deserialize<List<ShiftFileDTO>>(json);
}

// --- CustomFields helper ---

public static class CustomFieldsHelper
{
    public static void MapCustomFieldsToEntity(Dictionary<string, CustomField> srcFields, Dictionary<string, CustomField> destFields)
    {
        foreach (var key in srcFields.Keys)
        {
            var srcField = srcFields[key];

            // Skip password fields with null value (don't overwrite existing password)
            if (srcField.IsPassword && srcField.Value == null)
                continue;

            destFields[key] = new CustomField
            {
                Value = srcField.Value,
                DisplayName = srcField.DisplayName,
                IsPassword = srcField.IsPassword,
                IsEncrypted = srcField.IsEncrypted,
            };
        }
    }
}

#endregion

#region ===================== CONCRETE MAPPERS =========================

// --- Product Mapper ---

public class ProductMapper : IShiftEntityMapper<Product, ProductListDTO, ProductDTO>
{
    public ProductDTO ToViewDTO(Product entity)
    {
        var dto = new ProductDTO
        {
            ID = entity.ID.ToString(),
            Name = entity.Name,
            Description = entity.Description,
            Price = entity.Price,
            CreateDate = entity.CreateDate,
            LastSaveDate = entity.LastSaveDate,
            IsDeleted = entity.IsDeleted,
            Photos = ShiftFileConverter.Deserialize(entity.Photos),

            // Explicit FK → SelectDTO (alternative to ShiftEntitySelectDTOHelper)
            ProductCategory = new ShiftEntitySelectDTO
            {
                Value = entity.ProductCategoryID.ToString(),
                Text = entity.ProductCategory?.Name
            },
            ProductBrand = new ShiftEntitySelectDTO
            {
                Value = entity.ProductBrandID.ToString(),
                Text = entity.ProductBrand?.Name
            },
            CountryOfOrigin = entity.CountryOfOriginID.HasValue
                ? new ShiftEntitySelectDTO
                {
                    Value = entity.CountryOfOriginID.Value.ToString(),
                    Text = entity.CountryOfOrigin?.Name
                }
                : null,
        };

        return dto;
    }

    public Product ToEntity(ProductDTO dto, Product existing)
    {
        existing.Name = dto.Name;
        existing.Description = dto.Description;
        existing.Price = dto.Price;
        existing.Photos = ShiftFileConverter.Serialize(dto.Photos);

        // Explicit SelectDTO → FK
        existing.ProductCategoryID = long.Parse(dto.ProductCategory.Value);
        existing.ProductBrandID = long.Parse(dto.ProductBrand.Value);
        existing.CountryOfOriginID = dto.CountryOfOrigin != null && !string.IsNullOrWhiteSpace(dto.CountryOfOrigin.Value)
            ? long.Parse(dto.CountryOfOrigin.Value)
            : null;

        // Navigation properties are NOT touched — no risk of overwriting with null
        return existing;
    }

    public IQueryable<ProductListDTO> ProjectToList(IQueryable<Product> query)
    {
        // This Select() translates directly to SQL — no entity materialization
        return query.Select(p => new ProductListDTO
        {
            ID = p.ID.ToString(),
            Name = p.Name,
            Price = p.Price,
            IsDeleted = p.IsDeleted,
            ProductBrand = p.ProductBrand != null ? p.ProductBrand.Name : null,
            Category = p.ProductCategory != null ? p.ProductCategory.Name : null,
        });
    }

    public void CopyEntity(Product source, Product target)
    {
        target.Name = source.Name;
        target.Description = source.Description;
        target.Price = source.Price;
        target.Photos = source.Photos;
        target.ProductCategoryID = source.ProductCategoryID;
        target.ProductBrandID = source.ProductBrandID;
        target.CountryOfOriginID = source.CountryOfOriginID;
        target.ProductCategory = source.ProductCategory;
        target.ProductBrand = source.ProductBrand;
        target.CountryOfOrigin = source.CountryOfOrigin;
        target.CreateDate = source.CreateDate;
        target.LastSaveDate = source.LastSaveDate;
        target.IsDeleted = source.IsDeleted;
        // ReloadAfterSave is intentionally NOT copied
    }
}

// --- CompanyBranch Mapper (demonstrates collection + CustomFields) ---

public class CompanyBranchMapper : IShiftEntityMapper<CompanyBranch, ShiftEntityListDTO, CompanyBranchDTO>
{
    public CompanyBranchDTO ToViewDTO(CompanyBranch entity)
    {
        return new CompanyBranchDTO
        {
            ID = entity.ID.ToString(),
            Name = entity.Name,
            CreateDate = entity.CreateDate,
            LastSaveDate = entity.LastSaveDate,
            IsDeleted = entity.IsDeleted,

            // Junction table → List<ShiftEntitySelectDTO>
            Services = entity.CompanyBranchServices
                .Select(s => new ShiftEntitySelectDTO
                {
                    Value = s.ServiceID.ToString(),
                    Text = s.Service?.Name
                })
                .ToList(),

            CustomFields = new Dictionary<string, CustomField>(entity.CustomFields),
        };
    }

    public CompanyBranch ToEntity(CompanyBranchDTO dto, CompanyBranch existing)
    {
        existing.Name = dto.Name;

        // CustomFields with password-skip logic
        CustomFieldsHelper.MapCustomFieldsToEntity(dto.CustomFields, existing.CustomFields);

        // Collection sync: add/remove junction records
        // (In real code this would handle add/remove/update; simplified here)
        existing.CompanyBranchServices = dto.Services
            .Select(s => new CompanyBranchService
            {
                CompanyBranchID = existing.ID,
                ServiceID = long.Parse(s.Value),
            })
            .ToList();

        return existing;
    }

    public IQueryable<ShiftEntityListDTO> ProjectToList(IQueryable<CompanyBranch> query)
    {
        throw new NotImplementedException("Not needed for POC");
    }

    public void CopyEntity(CompanyBranch source, CompanyBranch target)
    {
        target.Name = source.Name;
        target.CustomFields = new Dictionary<string, CustomField>(source.CustomFields);
        target.CompanyBranchServices = source.CompanyBranchServices;
        target.CreateDate = source.CreateDate;
        target.LastSaveDate = source.LastSaveDate;
        target.IsDeleted = source.IsDeleted;
    }
}

// --- VehicleRepair Mapper (demonstrates NO implicit flattening) ---

public class VehicleRepairMapper : IShiftEntityMapper<VehicleRepair, ShiftEntityListDTO, VehicleRepairDTO>
{
    public VehicleRepairDTO ToViewDTO(VehicleRepair entity)
    {
        return new VehicleRepairDTO
        {
            ID = entity.ID.ToString(),
            RepairDescription = entity.RepairDescription,
            Vehicle = new ShiftEntitySelectDTO
            {
                Value = entity.VehicleID.ToString(),
                Text = entity.Vehicle?.Make
            },
            // VehicleVIN is NOT mapped here.
            // With AutoMapper, it WOULD be silently populated from entity.Vehicle.VIN.
            // With manual mapping, forgetting it means it stays null — no silent wrong data.
        };
    }

    public VehicleRepair ToEntity(VehicleRepairDTO dto, VehicleRepair existing)
    {
        existing.RepairDescription = dto.RepairDescription;
        existing.VehicleID = long.Parse(dto.Vehicle.Value);
        return existing;
    }

    public IQueryable<ShiftEntityListDTO> ProjectToList(IQueryable<VehicleRepair> query)
    {
        throw new NotImplementedException("Not needed for POC");
    }

    public void CopyEntity(VehicleRepair source, VehicleRepair target)
    {
        target.RepairDescription = source.RepairDescription;
        target.VehicleID = source.VehicleID;
        target.Vehicle = source.Vehicle;
        target.CreateDate = source.CreateDate;
        target.LastSaveDate = source.LastSaveDate;
        target.IsDeleted = source.IsDeleted;
    }
}

#endregion

#region ===================== REPOSITORY INTEGRATION SKETCH ============

// This shows how ShiftRepository would use the mapper interface
// instead of AutoMapper's IMapper.

public class RepositorySketch<TEntity, TListDTO, TViewDTO>
    where TEntity : ShiftEntity<TEntity>, new()
    where TListDTO : ShiftEntityListDTO
    where TViewDTO : ShiftEntityViewAndUpsertDTO
{
    private readonly IShiftEntityMapper<TEntity, TListDTO, TViewDTO> _mapper;

    public RepositorySketch(IShiftEntityMapper<TEntity, TListDTO, TViewDTO> mapper)
    {
        _mapper = mapper;
    }

    // Replaces: mapper.ProjectTo<ListDTO>(queryable.AsNoTracking())
    public IQueryable<TListDTO> OdataList(IQueryable<TEntity> queryable)
        => _mapper.ProjectToList(queryable);

    // Replaces: mapper.Map<ViewAndUpsertDTO>(entity)
    public TViewDTO View(TEntity entity)
        => _mapper.ToViewDTO(entity);

    // Replaces: mapper.Map(dto, entity)
    public TEntity Upsert(TViewDTO dto, TEntity entity)
        => _mapper.ToEntity(dto, entity);

    // Replaces: mapper.Map(freshEntity, trackedEntity) in ReloadAfterSaveTrigger
    public void ReloadAfterSave(TEntity source, TEntity target)
        => _mapper.CopyEntity(source, target);
}

#endregion

#region ======================== TESTS =================================

public class ManualMappingTests
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

        // FK → SelectDTO
        Assert.Equal("10", dto.ProductCategory.Value);
        Assert.Equal("Electronics", dto.ProductCategory.Text);
        Assert.Equal("20", dto.ProductBrand.Value);
        Assert.Equal("Acme", dto.ProductBrand.Text);
        Assert.NotNull(dto.CountryOfOrigin);
        Assert.Equal("30", dto.CountryOfOrigin!.Value);
        Assert.Equal("Germany", dto.CountryOfOrigin.Text);
    }

    // ---------------------------------------------------------------
    // Test 2b: ShiftEntitySelectDTO via reflection helper (opt-in)
    // ---------------------------------------------------------------
    [Fact]
    public void ShiftEntitySelectDTOViaReflectionHelper()
    {
        var entity = CreateSampleProduct();

        // Simulate a mapper that only maps scalars, then calls the helper
        var dto = new ProductDTO
        {
            ID = entity.ID.ToString(),
            Name = entity.Name,
            Price = entity.Price,
        };

        ShiftEntitySelectDTOHelper.MapFKsToSelectDTOs(entity, dto);

        Assert.Equal("10", dto.ProductCategory.Value);
        Assert.Equal("20", dto.ProductBrand.Value);
        // Note: Text is null because the helper only reads the FK ID, not the nav property
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

        var mapper = new ProductMapper();
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
        var existing = new Product { ID = 42 };

        var dto = new ProductDTO
        {
            Name = "Updated Widget",
            Description = "Updated description",
            Price = 24.99m,
            ProductCategory = new ShiftEntitySelectDTO { Value = "10" },
            ProductBrand = new ShiftEntitySelectDTO { Value = "20" },
            CountryOfOrigin = new ShiftEntitySelectDTO { Value = "30" },
        };

        var result = mapper.ToEntity(dto, existing);

        Assert.Equal(42, result.ID); // ID preserved
        Assert.Equal("Updated Widget", result.Name);
        Assert.Equal(24.99m, result.Price);
        Assert.Equal(10, result.ProductCategoryID);
        Assert.Equal(20, result.ProductBrandID);
        Assert.Equal(30, result.CountryOfOriginID);

        // Navigation properties are NOT touched
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
        var existingFields = new Dictionary<string, CustomField>
        {
            ["apiKey"] = new CustomField { Value = "secret-123", DisplayName = "API Key", IsPassword = true },
            ["label"] = new CustomField { Value = "Old Label", DisplayName = "Label" },
        };

        var dtoFields = new Dictionary<string, CustomField>
        {
            // Password field with null value — should NOT overwrite existing
            ["apiKey"] = new CustomField { Value = null, DisplayName = "API Key", IsPassword = true },
            // Regular field — should overwrite
            ["label"] = new CustomField { Value = "New Label", DisplayName = "Label" },
        };

        CustomFieldsHelper.MapCustomFieldsToEntity(dtoFields, existingFields);

        Assert.Equal("secret-123", existingFields["apiKey"].Value); // preserved
        Assert.Equal("New Label", existingFields["label"].Value);   // updated
    }

    // ---------------------------------------------------------------
    // Test 7: Type converter (List<ShiftFileDTO> ↔ string)
    // ---------------------------------------------------------------
    [Fact]
    public void TypeConverterShiftFileDTO()
    {
        var files = new List<ShiftFileDTO>
        {
            new() { Name = "photo1.jpg", Blob = "base64data", Size = 1024 },
            new() { Name = "photo2.png", Blob = "moredata", Size = 2048 },
        };

        var json = ShiftFileConverter.Serialize(files);
        Assert.NotNull(json);
        Assert.Contains("photo1.jpg", json);

        var deserialized = ShiftFileConverter.Deserialize(json);
        Assert.NotNull(deserialized);
        Assert.Equal(2, deserialized!.Count);
        Assert.Equal("photo1.jpg", deserialized[0].Name);
        Assert.Equal(2048, deserialized[1].Size);

        // Null/empty handling
        Assert.Null(ShiftFileConverter.Serialize(null));
        Assert.Empty(ShiftFileConverter.Deserialize(null)!);
        Assert.Empty(ShiftFileConverter.Deserialize("")!);
    }

    // ---------------------------------------------------------------
    // Test 8: VehicleVIN — NO implicit flattening
    // ---------------------------------------------------------------
    [Fact]
    public void VehicleVINNoImplicitFlattening()
    {
        var entity = new VehicleRepair
        {
            ID = 1,
            RepairDescription = "Brake pad replacement",
            VehicleID = 50,
            Vehicle = new Vehicle { ID = 50, VIN = "1HGBH41JXMN109186", Make = "Honda" }
        };

        var mapper = new VehicleRepairMapper();
        var dto = mapper.ToViewDTO(entity);

        // The SelectDTO for Vehicle is mapped correctly
        Assert.Equal("50", dto.Vehicle.Value);
        Assert.Equal("Honda", dto.Vehicle.Text);

        // VehicleVIN is NOT silently populated from Vehicle.VIN.
        // With AutoMapper, this WOULD be "1HGBH41JXMN109186" due to flattening convention.
        Assert.Null(dto.VehicleVIN);
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
            ReloadAfterSave = true, // should NOT be overwritten
        };

        var mapper = new ProductMapper();
        mapper.CopyEntity(fresh, tracked);

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
