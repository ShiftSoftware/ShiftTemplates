using ShiftMapper;
using StockPlusPlus.Data.Entities;
using StockPlusPlus.Shared.DTOs;
using StockPlusPlus.Shared.DTOs.Invoice;
using StockPlusPlus.Shared.DTOs.ProductBrand;

namespace StockPlusPlus.Data.Mappers;

/// <summary>
/// The ONE place this project customizes its automatic maps. Every repository's four maps (entity ↔ view,
/// entity → list, entity → entity) — and every attribute-driven endpoint's — are declared by ShiftMapper from the
/// type arguments, in this project's build, with the framework's conventions (hash ids, <c>ShiftEntitySelectDTO</c>,
/// files, the members the pipeline owns); most entities need nothing written (see <c>CountryRepository</c>,
/// <c>ProductCategoryRepository</c>, the <c>api/country</c> endpoint). What convention cannot do is written HERE:
/// an ordinary ShiftMapper class — not partial, no attribute, no interface, nothing injects it, nothing registers
/// it by name (<c>RegisterShiftRepositories</c> registers this assembly's generated mapper, which carries these
/// maps). Each <c>CreateMap</c> REPLACES the automatic map for that pair — the build says so, SM0047,
/// informational — and every other pair of the triple stays automatic, the nested pairs below a replaced map
/// included. The framework's conventions still apply to the maps written here.
/// <para>
/// Nothing about mapping is written in a repository or an entity: what a member maps from is not their business.
/// A repository's only word about its maps is how DEEP they nest — <c>o.Mapping(m =&gt; m.Nested(n))</c> — and
/// the same maps serve any service that injects <c>Mapper</c> (typed methods) or <c>IMapper</c>. The two doors
/// that are not this class: a repository overriding <c>MapToView</c>/<c>MapToEntity</c>/<c>MapToList</c>
/// (<c>ProductRepository</c>), and a hand-written <c>IShiftEntityMapper</c> on a <c>WithMapper</c> endpoint
/// (<c>CountryMapper</c>, <c>api/countrymapped</c>).
/// </para>
/// </summary>
public class StockPlusPlusMapper : ShiftMapperBase
{
    public StockPlusPlusMapper()
    {
#if (includeItemTemplateContent)
        // ── ProductBrand ─────────────────────────────────────────────────────────────────────────────────────
        // LIST — per-member customization, in ShiftMapper's vocabulary. ForMember with an expression composes
        // into the single SQL projection of the list; everything else stays by convention.
        CreateMap<ProductBrand, ProductBrandListDTO>()
            .ForMember(d => d.Code, opt => opt.MapFrom(entity => entity.Code ?? "(No Code)"));

        // WRITE — "conventions first, then my code": AfterMap runs after every convention has written the
        // entity — the mapper-class analog of a repository override that calls base.MapToEntity(...) first.
        CreateMap<ProductBrandDTO, ProductBrand>()
            .AfterMap((dto, entity) => entity.Code = entity.Code?.Trim());
#endif

        // ── Invoice ──────────────────────────────────────────────────────────────────────────────────────────
        // LIST — InvoiceListDTO.Total has no column, so the list map is told to sum the lines; an expression, so
        // it runs in SQL. The lines below the list map still nest with nothing written (InvoiceLine →
        // InvoiceLineListDTO → its Product): the pairs BELOW a replaced map are still the framework's to declare.
        // Being the map itself, the customization applies wherever the pair is mapped — the endpoint, a report
        // service injecting Mapper — with nothing to construct first.
        CreateMap<Invoice, InvoiceListDTO>()
            .ForMember(d => d.Total, opt => opt.MapFrom(i => i.InvoiceLines.Sum(l => l.Price)));

        // ── Country (api/country-generated) ──────────────────────────────────────────────────────────────────
        // An ATTRIBUTE-DRIVEN endpoint's automatic map, customized exactly as a repository's: api/country-generated
        // has no repository class — its maps are declared by the [ShiftEntitySecureEndpoint<CountryGeneratedDTO,
        // CountryGeneratedDTO, …>] on Country — and this CreateMap replaces one of them. The entity's
        // ConfigureRepository has no say in it. One DTO type is both list and view there, so Country →
        // CountryGeneratedDTO is ONE map (Q15 of the plan): the suffix shows on the view and in the list projection
        // alike. The write and copy maps stay automatic; the plain api/country endpoint (CountryDTO) and
        // api/countrymapped (CountryMapper) are untouched — maps are keyed by DTO type.
        CreateMap<Country, CountryGeneratedDTO>()
            .ForMember(d => d.Name, opt => opt.MapFrom(e => e.Name + " (via StockPlusPlusMapper)"));
    }
}
