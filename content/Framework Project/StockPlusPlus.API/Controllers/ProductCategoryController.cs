using AutoMapper;
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
    private readonly IMapper mapper;

    public ProductCategoryController(
        ProductCategoryRepository repository, 
        IConfiguration configuration, 
        DB db,
        IDefaultDataLevelAccess defaultDataLevelAccess,
        IMapper mapper
    ) : base(StockPlusPlusActionTree.ProductCategory)
    {
        this.repository = repository;
        this.configuration = configuration;
        this.db = db;
        this.defaultDataLevelAccess = defaultDataLevelAccess;
        this.mapper = mapper;
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

        var result = await mapper
            .ProjectTo<ProductCategoryListDTO>(query)
            .ApplyDefaultSoftDeleteFilter(oDataQueryOptions)
            .ToOdataDTO(oDataQueryOptions, this.Request);

        return Ok(result);
    }
}