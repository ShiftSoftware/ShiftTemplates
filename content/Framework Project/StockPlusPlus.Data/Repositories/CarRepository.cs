using ShiftSoftware.ShiftEntity.EFCore;
using StockPlusPlus.Data.DbContext;
using StockPlusPlus.Data.Entities;
using StockPlusPlus.Shared.DTOs.Car;

namespace StockPlusPlus.Data.Repositories;

/// <summary>
/// Mapping is AUTOMATIC: ShiftMapper declares the four maps from the type arguments, so nothing is written here.
/// Access is not the repository's concern either — <c>CarController</c> gates it with <c>StockPlusPlusActionTree.Car</c>.
/// </summary>
public class CarRepository : ShiftRepository<DB, Car, CarListDTO, CarDTO>
{
    public CarRepository(DB db) : base(db)
    {
    }
}
