using Mapster;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using System.Reflection;

namespace StockPlusPlus.Data.Mappers;

/// <summary>
/// Default Mapster configuration for ShiftEntity base class properties and conventions.
/// Mirrors what DefaultAutoMapperProfile does for AutoMapper:
/// - Ignores base/infrastructure properties automatically
/// - FK ↔ ShiftEntitySelectDTO by naming convention ({Name}ID)
/// - ShiftFileDTO ↔ string JSON conversion
/// - Navigation property circular reference prevention
/// </summary>
public static class MapsterShiftEntityDefaults
{
    private static readonly HashSet<string> EntityInternalProperties = new()
    {
        nameof(ShiftEntity<object>.ReloadAfterSave),
        nameof(ShiftEntity<object>.AuditFieldsAreSet),
        nameof(ShiftEntity<object>.CreateDate),
        nameof(ShiftEntity<object>.LastSaveDate),
        nameof(ShiftEntity<object>.IsDeleted),
        nameof(ShiftEntity<object>.CreatedByUserID),
        nameof(ShiftEntity<object>.LastSavedByUserID),
        nameof(ShiftEntityBase.ID),
        "LastReplicationDate",
        "RegionID", "CompanyID", "CompanyBranchID",
        "CityID", "CountryID", "IdempotencyKey",
        "TeamID", "BrandID",
    };

    public static TypeAdapterConfig CreateConfig()
    {
        var config = new TypeAdapterConfig();
        config.Default.PreserveReference(true);

        // Global type converters — same as AutoMapper's CreateMap + ConvertUsing
        config.NewConfig<string?, List<ShiftFileDTO>?>()
            .MapWith(src => string.IsNullOrWhiteSpace(src)
                ? new List<ShiftFileDTO>()
                : System.Text.Json.JsonSerializer.Deserialize<List<ShiftFileDTO>>(src) ?? new List<ShiftFileDTO>());

        config.NewConfig<List<ShiftFileDTO>?, string?>()
            .MapWith(src => src == null ? null : System.Text.Json.JsonSerializer.Serialize(src));

        // long/long? → string conversions for ID and FK fields in list DTOs
        config.NewConfig<long, string>()
            .MapWith(src => src.ToString());

        config.NewConfig<long?, string?>()
            .MapWith(src => src.HasValue ? src.Value.ToString() : null);

        return config;
    }

    /// <summary>
    /// Entity → ViewDTO: ignores base audit fields (handled by MapBaseFields),
    /// and auto-populates ShiftEntitySelectDTO properties from FK by convention.
    /// </summary>
    public static TypeAdapterSetter<TEntity, TViewDTO> EntityToView<TEntity, TViewDTO>(
        this TypeAdapterConfig config)
        where TEntity : ShiftEntity<TEntity>
        where TViewDTO : ShiftEntityViewAndUpsertDTO
    {
        return config.NewConfig<TEntity, TViewDTO>()
            .Ignore(d => d.ID!, d => d.IsDeleted, d => d.CreateDate, d => d.LastSaveDate,
                    d => d.CreatedByUserID!, d => d.LastSavedByUserID!)
            .AfterMapping((entity, dto) => MapEntityFKsToSelectDTOs(entity, dto));
    }

    /// <summary>
    /// ViewDTO → Entity: ignores base/infrastructure properties,
    /// ignores virtual nav properties, and auto-sets FK values from ShiftEntitySelectDTO.
    /// </summary>
    public static TypeAdapterSetter<TViewDTO, TEntity> ViewToEntity<TViewDTO, TEntity>(
        this TypeAdapterConfig config)
        where TEntity : ShiftEntity<TEntity>
        where TViewDTO : ShiftEntityViewAndUpsertDTO
    {
        return config.NewConfig<TViewDTO, TEntity>()
            .IgnoreMember((member, side) =>
                side == MemberSide.Destination && EntityInternalProperties.Contains(member.Name))
            .IgnoreVirtualProperties()
            .AfterMapping((dto, entity) => MapSelectDTOsToEntityFKs(dto, entity));
    }

    /// <summary>
    /// Entity → ListDTO for IQueryable projection via ProjectToType.
    /// Mapster flattening handles nav property names (e.g., ProductBrand.Name → ProductBrand).
    /// long→string converters handle ID/FK fields.
    /// Returns the setter for entity-specific overrides (e.g., mismatched property names).
    /// </summary>
    public static TypeAdapterSetter<TEntity, TListDTO> EntityToList<TEntity, TListDTO>(
        this TypeAdapterConfig config)
        where TEntity : ShiftEntity<TEntity>
        where TListDTO : ShiftEntityListDTO
    {
        return config.NewConfig<TEntity, TListDTO>();
    }

    /// <summary>
    /// Entity → Entity copy: ignores ID, ReloadAfterSave, AuditFieldsAreSet, and nav properties.
    /// </summary>
    public static TypeAdapterSetter<TEntity, TEntity> EntityCopy<TEntity>(
        this TypeAdapterConfig config)
        where TEntity : ShiftEntity<TEntity>
    {
        return config.NewConfig<TEntity, TEntity>()
            .Ignore(d => d.ID, d => d.ReloadAfterSave, d => d.AuditFieldsAreSet)
            .IgnoreVirtualProperties();
    }

    /// <summary>
    /// Convention: for each ShiftEntitySelectDTO property on the DTO,
    /// find the FK on the entity by {PropertyName}ID and populate Value + Text.
    /// Mirrors DefaultAutoMapperProfile.DefaultEntityToDtoAfterMap.
    /// </summary>
    private static void MapEntityFKsToSelectDTOs(object entity, object dto)
    {
        var entityType = entity.GetType();

        foreach (var dtoProp in dto.GetType().GetProperties())
        {
            if (dtoProp.PropertyType == typeof(ShiftEntitySelectDTO) ||
                dtoProp.PropertyType == typeof(ShiftEntitySelectDTO))
            {
                var isNullable = dtoProp.PropertyType == typeof(ShiftEntitySelectDTO) ? false :
                    Nullable.GetUnderlyingType(dtoProp.PropertyType) != null;

                var fkName = $"{dtoProp.Name}ID";
                var fkProp = entityType.GetProperty(fkName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

                if (fkProp != null)
                {
                    var fkValue = fkProp.GetValue(entity);

                    // Nullable FK with null value → null SelectDTO
                    if (fkValue == null)
                    {
                        dtoProp.SetValue(dto, null);
                        continue;
                    }

                    string value = fkValue.ToString()!;
                    string? text = null;

                    // Try to get Text from nav property (same name as DTO property)
                    var navProp = entityType.GetProperty(dtoProp.Name, BindingFlags.Public | BindingFlags.Instance);
                    if (navProp != null)
                    {
                        var navEntity = navProp.GetValue(entity);
                        if (navEntity != null)
                        {
                            var keyAndName = (ShiftEntityKeyAndNameAttribute?)
                                Attribute.GetCustomAttribute(navEntity.GetType(), typeof(ShiftEntityKeyAndNameAttribute));

                            if (keyAndName != null)
                            {
                                text = navEntity.GetType().GetProperty(keyAndName.Text)?.GetValue(navEntity)?.ToString();
                            }
                            else
                            {
                                // Fallback: try "Name" property
                                text = navEntity.GetType().GetProperty("Name")?.GetValue(navEntity)?.ToString();
                            }
                        }
                    }

                    dtoProp.SetValue(dto, new ShiftEntitySelectDTO { Value = value, Text = text });
                }
            }
        }
    }

    /// <summary>
    /// Convention: for each ShiftEntitySelectDTO property on the DTO,
    /// find the FK on the entity by {PropertyName}ID and set it from Value.
    /// Mirrors DefaultAutoMapperProfile.DefaultDtoToEntityAfterMap.
    /// </summary>
    private static void MapSelectDTOsToEntityFKs(object dto, object entity)
    {
        var entityType = entity.GetType();

        foreach (var dtoProp in dto.GetType().GetProperties())
        {
            if (!typeof(ShiftEntitySelectDTO).IsAssignableFrom(dtoProp.PropertyType) &&
                !(dtoProp.PropertyType.IsGenericType &&
                  dtoProp.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>)))
                continue;

            if (dtoProp.PropertyType != typeof(ShiftEntitySelectDTO) &&
                dtoProp.PropertyType != typeof(ShiftEntitySelectDTO))
                continue;

            var selectDTO = (ShiftEntitySelectDTO?)dtoProp.GetValue(dto);

            var fkName = $"{dtoProp.Name}ID";
            var fkProp = entityType.GetProperty(fkName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

            if (fkProp == null)
                continue;

            if (selectDTO == null || string.IsNullOrWhiteSpace(selectDTO.Value))
            {
                // Nullable FK → set to null
                if (Nullable.GetUnderlyingType(fkProp.PropertyType) != null)
                    fkProp.SetValue(entity, null);
            }
            else
            {
                fkProp.SetValue(entity, long.Parse(selectDTO.Value));
            }
        }
    }

    private static TypeAdapterSetter<TSource, TTarget> IgnoreVirtualProperties<TSource, TTarget>(
        this TypeAdapterSetter<TSource, TTarget> setter)
    {
        return setter.IgnoreMember((member, side) =>
        {
            if (side != MemberSide.Destination)
                return false;

            var prop = typeof(TTarget).GetProperty(member.Name);
            if (prop == null)
                return false;

            var getter = prop.GetGetMethod();
            return getter != null && getter.IsVirtual && !getter.IsFinal;
        });
    }
}
