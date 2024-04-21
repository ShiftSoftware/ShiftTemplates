using Microsoft.AspNetCore.Mvc;
using ShiftSoftware.ShiftEntity.Web;
using StockPlusPlus.Data.Repositories.Product;
using StockPlusPlus.Shared.ActionTrees;
using StockPlusPlus.Shared.DTOs.Product.Product;
using StockPlusPlus.Shared.DTOs.Product.ProductCategory;

namespace StockPlusPlus.API.Controllers.Product;

[Route("api/[controller]")]
public class ProductController : ShiftEntitySecureControllerAsync<ProductRepository, Data.Entities.Product.Product, ProductListDTO, ProductDTO>
{
    public ProductController() : base(StockActionTrees.Product, x =>
    {
        x.FilterBy(x => x.ID, StockActionTrees.DataLevelAccess.ProductCategory)
        .DecodeHashId<ProductCategoryListDTO>()
        .IncludeCreatedByCurrentUser(x => x.CreatedByUserID);

        x.FilterBy(x => x.ID, StockActionTrees.DataLevelAccess.ProductBrand)
        .IncludeCreatedByCurrentUser(x => x.CreatedByUserID);
    }
    )
    {

    }
}
