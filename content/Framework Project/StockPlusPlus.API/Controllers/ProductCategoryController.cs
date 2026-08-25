using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Web;
using ShiftSoftware.ShiftEntity.Web.Services;
using ShiftSoftware.TypeAuth.AspNetCore;
using ShiftSoftware.TypeAuth.Core;
using StockPlusPlus.Data.DbContext;
using StockPlusPlus.Data.Repositories;
using StockPlusPlus.Shared.ActionTrees;
using StockPlusPlus.Shared.DTOs.ProductCategory;

namespace StockPlusPlus.API.Controllers;

[Route("api/[controller]")]
public class ProductCategoryController : ShiftEntitySecureControllerAsync<ProductCategoryRepository, Data.Entities.ProductCategory, ProductCategoryListDTO, ProductCategoryDTO>
{
    private readonly ProductCategoryRepository repository;
    private readonly IConfiguration configuration;
    private readonly DB db;
    private readonly IDefaultDataLevelAccess defaultDataLevelAccess;

    public ProductCategoryController(
        ProductCategoryRepository repository, 
        IConfiguration configuration, 
        DB db,
        IDefaultDataLevelAccess defaultDataLevelAccess
    ) : base(StockPlusPlusActionTree.ProductCategory)
    {
        this.repository = repository;
        this.configuration = configuration;
        this.db = db;
        this.defaultDataLevelAccess = defaultDataLevelAccess;
    }

    [HttpGet("custom-list")]
    [TypeAuth<StockPlusPlusActionTree>(nameof(StockPlusPlusActionTree.ProductCategory), Access.Read)]
    public async Task<IActionResult> CustomList([FromQuery] ODataQueryOptions<ProductCategoryListDTO> oDataQueryOptions)
    {
        var query = await db.ProductCategories
            .ApplyDefaultDataLevelAccessFilters(
                this.defaultDataLevelAccess, 
                this.repository.ShiftRepositoryOptions.DefaultDataLevelAccessOptions
            )
            .ApplyGlobalRepositoryFiltersAsync(this.repository.ShiftRepositoryOptions.GlobalRepositoryFilters);

        // The repository's own list projection — the source-generated one it uses for api/productcategory —
        // rather than a second, separately-maintained description of the same shape.
        var result = await this.repository
            .MapToList(query)
            .ToOdataDTO(oDataQueryOptions, this.Request);

        return Ok(result);
    }
}