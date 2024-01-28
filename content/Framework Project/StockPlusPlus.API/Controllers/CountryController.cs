using Microsoft.AspNetCore.Mvc;
using ShiftSoftware.ShiftEntity.Web;
using StockPlusPlus.Data.Repositories;
using StockPlusPlus.Shared.ActionTrees;
using StockPlusPlus.Shared.DTOs;

namespace StockPlusPlus.API.Controllers;

[Route("api/[controller]")]
public class CountryController : ShiftEntitySecureControllerAsync<CountryRepository, Data.Entities.Country, CountryDTO, CountryDTO>
{
    public CountryController() : base(StockActionTrees.Country)
    {
    }
}
