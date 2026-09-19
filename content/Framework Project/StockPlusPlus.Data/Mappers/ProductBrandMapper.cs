using ShiftMapper;
using StockPlusPlus.Data.Entities;
using StockPlusPlus.Shared.DTOs.ProductBrand;

namespace StockPlusPlus.Data.Mappers;

/// <summary>
/// ProductBrand demonstrates the MAPPER CLASS door (the customization path). An ordinary ShiftMapper class:
/// not partial, no attribute, no interface, nothing injects it. A <c>CreateMap</c> here REPLACES the automatic
/// map ShiftMapper declared for that pair from <c>ProductBrandRepository</c>'s type arguments — the build says so
/// (SM0047, informational) — and every other pair of the triple stays automatic. The framework's conventions
/// (hash ids, <c>ShiftEntitySelectDTO</c>, the members the pipeline owns) still apply to the maps written here.
/// Entities with nothing custom need no class at all — see Country (api/country-generated + CountryRepository).
/// </summary>
public class ProductBrandMapper : ShiftMapperBase
{
    public ProductBrandMapper()
    {
#if (includeItemTemplateContent)
        // Per-member customization, in ShiftMapper's vocabulary. ForMember with an expression composes into
        // the single SQL projection of the list; everything else stays by convention.
        CreateMap<ProductBrand, ProductBrandListDTO>()
            .ForMember(d => d.Code, opt => opt.MapFrom(entity => entity.Code ?? "(No Code)"));

        // "Conventions first, then my code": AfterMap runs after every convention has written the entity —
        // the mapper-class analog of a repository override that calls base.MapToEntity(...) first.
        CreateMap<ProductBrandDTO, ProductBrand>()
            .AfterMap((dto, entity) => entity.Code = entity.Code?.Trim());
#else
        // Every member maps by convention; the class exists so there is a place to customize. Per member:
        //
        //CreateMap<ProductBrand, ProductBrandListDTO>()
        //    .ForMember(d => d.SomeColumn, opt => opt.MapFrom(entity => entity.SomeColumn ?? "(none)"));
        //
        // Or the conventions first, then your code, on the write direction:
        //
        //CreateMap<ProductBrandDTO, ProductBrand>()
        //    .AfterMap((dto, entity) => entity.Code = entity.Code?.Trim());
#endif
    }
}
