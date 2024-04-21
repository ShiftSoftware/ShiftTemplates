using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShiftSoftware.ShiftEntity.Web;
using StockPlusPlus.Data.Repositories.Product;
using StockPlusPlus.Shared.ActionTrees;
using StockPlusPlus.Shared.DTOs.Product.ProductCategory;
using System.Security.Cryptography;
using System.Text;

namespace StockPlusPlus.API.Controllers.Product;

[Route("api/[controller]")]
public class ProductCategoryController : ShiftEntitySecureControllerAsync<ProductCategoryRepository, Data.Entities.Product.ProductCategory, ProductCategoryListDTO, ProductCategoryDTO>
{
    private readonly ProductCategoryRepository repository;
    private readonly IConfiguration configuration;

    public ProductCategoryController(ProductCategoryRepository repository, IConfiguration configuration) : base(StockActionTrees.ProductCategory, x =>
        x.FilterBy(x => x.ID, StockActionTrees.DataLevelAccess.ProductCategory)
        .DecodeHashId<ProductCategoryListDTO>()
        .IncludeCreatedByCurrentUser(x => x.CreatedByUserID)
    )
    {
        this.repository = repository;
        this.configuration = configuration;
    }
}
