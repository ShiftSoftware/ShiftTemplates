using ShiftSoftware.ShiftEntity.EFCore;
using StockPlusPlus.Data.DbContext;
using StockPlusPlus.Shared.DTOs;

namespace StockPlusPlus.Data.Repositories;

/// <summary>
/// Demonstrates SOURCE-GENERATED mapping with ZERO mapping code: the source generator auto-discovers
/// this repository's (Country, CountryRepoDTO, CountryRepoDTO) triple, emits a mapper for it,
/// and UseGeneratedMapper() resolves it from the registry — no mapper class is declared anywhere.
/// Note: the attribute-driven api/country* endpoints do NOT use this repository (they use the framework's
/// built-in repository); this is a plain repository to inject and use directly.
/// <para>
/// It owns its own DTO triple on purpose. Passing an options builder to the base constructor (as below) means
/// this repository configures ITSELF and takes over completely, so an entity's IConfiguresShiftRepository for
/// the same triple would silently never run — the generator warns about that pairing (SHENGEN006). Country
/// configures the CountryGeneratedDTO triple from the entity, so this demo stays clear of it.
/// </para>
/// </summary>
public class CountryRepository : ShiftRepository<DB, Entities.Country, CountryRepoDTO, CountryRepoDTO>
{
    public CountryRepository(DB db) : base(db, x => x.UseGeneratedMapper())
    {
    }
}
