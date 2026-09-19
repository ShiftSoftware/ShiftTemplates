using ShiftSoftware.ShiftEntity.EFCore;
using StockPlusPlus.Data.DbContext;
using StockPlusPlus.Shared.DTOs;

namespace StockPlusPlus.Data.Repositories;

/// <summary>
/// Demonstrates AUTOMATIC mapping with ZERO mapping code: closing <c>ShiftRepository&lt;DB, Country, CountryRepoDTO,
/// CountryRepoDTO&gt;</c> is all it takes — ShiftMapper's generator declares the four maps for the triple
/// (entity ↔ view, entity → list, entity → entity) in this project's build, with the framework's conventions
/// (hash ids, <c>ShiftEntitySelectDTO</c>, files, the members the pipeline owns), and the repository resolves
/// them through the host's <c>IMapper</c>. No mapper class, no configuration, no attribute. The same maps are
/// available to any service that injects <c>Mapper</c>: <c>mapper.MapToCountryRepoDTO(country)</c>.
/// Note: the attribute-driven api/country* endpoints do NOT use this repository (they use the framework's
/// built-in repository); this is a plain repository to inject and use directly.
/// <para>
/// It owns its own DTO triple on purpose: a pair is customized in ONE place (a repository's <c>Mapping(...)</c>
/// or an entity's <c>ConfigureRepository</c>), and the build refuses two (SM0050). Country configures the
/// CountryGeneratedDTO triple from the entity, so this demo stays clear of it.
/// </para>
/// </summary>
public class CountryRepository : ShiftRepository<DB, Entities.Country, CountryRepoDTO, CountryRepoDTO>
{
    public CountryRepository(DB db) : base(db)
    {
    }
}
