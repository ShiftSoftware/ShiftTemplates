using Microsoft.AspNetCore.Mvc;
using ShiftSoftware.ShiftEntity.Web;
using StockPlusPlus.Data.Repositories;
using StockPlusPlus.Shared.ActionTrees;
using StockPlusPlus.Shared.DTOs.Car;

namespace StockPlusPlus.API.Controllers;

/// <summary>
/// Passing the action (instead of <c>null</c>) is what turns the gates on: the secure controller checks
/// CanRead for the list and a single item, CanWrite for insert and update, and CanDelete for delete,
/// and answers 403 when the caller's access tree does not grant it.
/// </summary>
[Route("api/[controller]")]
public class CarController : ShiftEntitySecureControllerAsync<CarRepository, Data.Entities.Car, CarListDTO, CarDTO>
{
    public CarController() : base(StockPlusPlusActionTree.Car)
    {
    }
}
