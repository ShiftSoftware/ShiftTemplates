using Microsoft.AspNetCore.Mvc;
using ShiftSoftware.ShiftEntity.Web;
using StockPlusPlus.Data.Repositories;
using StockPlusPlus.Shared.ActionTrees;
using StockPlusPlus.Shared.DTOs.Product;

namespace StockPlusPlus.API.Controllers;

[Route("api/[controller]")]
public class ProductController : ShiftEntitySecureControllerAsync<ProductRepository, Data.Entities.Product, ProductListDTO, ProductDTO>
{
    public ProductController() : base(StockPlusPlusActionTree.Product)
    {

    }
}