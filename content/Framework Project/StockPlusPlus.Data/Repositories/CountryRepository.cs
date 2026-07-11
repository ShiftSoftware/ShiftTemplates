using ShiftSoftware.ShiftEntity.EFCore;
using StockPlusPlus.Data.DbContext;
using StockPlusPlus.Shared.DTOs;

namespace StockPlusPlus.Data.Repositories;

/// <summary>
/// Demonstrates SOURCE-GENERATED mapping with ZERO mapping code: the source generator auto-discovers
/// this repository's (Country, CountryGeneratedDTO, CountryGeneratedDTO) triple, emits a mapper for it,
/// and UseGeneratedMapper() resolves it from the registry — no mapper class is declared anywhere.
/// Note: the attribute-driven api/country* endpoints do NOT use this repository (they use the framework's
/// built-in repository); this is a plain repository to inject and use directly.
/// </summary>
public class CountryRepository : ShiftRepository<DB, Entities.Country, CountryGeneratedDTO, CountryGeneratedDTO>
{
    public CountryRepository(DB db) : base(db, x => x.UseGeneratedMapper())
    {
    }
}
