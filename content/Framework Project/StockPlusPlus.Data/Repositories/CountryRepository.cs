using ShiftSoftware.ShiftEntity.EFCore;
using StockPlusPlus.Data.DbContext;
using StockPlusPlus.Data.Entities;
using StockPlusPlus.Shared.DTOs;

namespace StockPlusPlus.Data.Repositories;

public class CountryRepository : ShiftRepository<DB, Country, CountryDTO, CountryDTO>
{
    public CountryRepository(DB db, AutoMapper.IMapper mapper) : base(db)
    {

    }
}
