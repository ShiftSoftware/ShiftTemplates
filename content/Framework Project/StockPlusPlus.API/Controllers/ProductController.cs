using Microsoft.AspNetCore.Mvc;
using ShiftSoftware.ShiftEntity.Web;
using StockPlusPlus.Data.Repositories;
using StockPlusPlus.Shared.ActionTrees;
using StockPlusPlus.Shared.DTOs.Product;
using StockPlusPlus.Shared.DTOs.ProductCategory;

namespace StockPlusPlus.API.Controllers;

[Route("api/[controller]")]
public class ProductController : ShiftEntitySecureControllerAsync<ProductRepository, Data.Entities.Product, ProductListDTO, ProductDTO>
{
    public ProductController() : base(StockPlusPlusActionTree.Product, x =>
    {
        //x.FilterBy(x => x.ID, StockPlusPlusActionTree.DataLevelAccess.ProductCategory)
        //.DecodeHashId<ProductCategoryListDTO>()
        //.IncludeCreatedByCurrentUser(x => x.CreatedByUserID);

        //x.FilterBy(x => x.ID, StockPlusPlusActionTree.DataLevelAccess.ProductBrand)
        //.IncludeCreatedByCurrentUser(x => x.CreatedByUserID);
    }
    )
    {

    }
}
