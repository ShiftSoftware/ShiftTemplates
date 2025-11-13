using Microsoft.AspNetCore.Mvc;
using ShiftSoftware.ShiftEntity.Web;
using StockPlusPlus.Data.Repositories;
using StockPlusPlus.Shared.ActionTrees;
using StockPlusPlus.Shared.DTOs.Invoice;

namespace StockPlusPlus.API.Controllers;

[Route("api/[controller]")]
public class InvoiceController : ShiftEntitySecureControllerAsync<InvoiceRepository, Data.Entities.Invoice, InvoiceListDTO, InvoiceDTO>
{
    public InvoiceController() : base(StockPlusPlusActionTree.Invoice)
    {

    }
}