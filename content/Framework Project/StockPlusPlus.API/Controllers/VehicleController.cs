using Microsoft.AspNetCore.Mvc;
using ShiftSoftware.ShiftEntity.Web;
using StockPlusPlus.Data.Repositories;
using StockPlusPlus.Shared.ActionTrees;
using StockPlusPlus.Shared.DTOs.Vehicle;

namespace StockPlusPlus.API.Controllers;

[Route("api/[controller]")]
public class VehicleController :
    ShiftEntitySecureControllerAsync<VehicleRepository, Data.Entities.Vehicle, VehicleListDTO, VehicleDTO>
{
    public VehicleController() : base(StockPlusPlusActionTree.Vehicle)
    {
    }
}
