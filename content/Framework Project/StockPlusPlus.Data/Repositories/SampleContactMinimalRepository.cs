using ShiftSoftware.ShiftEntity.EFCore;
using StockPlusPlus.Data.DbContext;
using StockPlusPlus.Data.Entities;
using StockPlusPlus.Shared.DTOs.SampleContact;

namespace StockPlusPlus.Data.Repositories;

public class SampleContactMinimalRepository(DB db)
    : ShiftRepository<DB, SampleContact, SampleContactMinimalListDTO, SampleContactMinimalDTO>(db);
