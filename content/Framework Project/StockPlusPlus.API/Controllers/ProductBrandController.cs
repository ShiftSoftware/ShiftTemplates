using Microsoft.AspNetCore.Mvc;
using ShiftSoftware.ShiftEntity.Web;
using StockPlusPlus.Data.Repositories;
using StockPlusPlus.Shared.DTOs.ProductBrand;
#if (includeItemTemplateContent)
using Microsoft.AspNetCore.Authorization;
using StockPlusPlus.Shared.ActionTrees;
#endif

namespace StockPlusPlus.API.Controllers;

[Route("api/[controller]")]
public class ProductBrandController : ShiftEntitySecureControllerAsync<ProductBrandRepository, Data.Entities.ProductBrand, ProductBrandListDTO, ProductBrandDTO>
{
#if (includeItemTemplateContent)
    private readonly ProductBrandRepository brandRepository;
    private readonly ProductCategoryRepository productCategoryRepository;
    public ProductBrandController(ProductBrandRepository brandRepository, ProductCategoryRepository productCategoryRepository) : base(StockPlusPlusActionTree.ProductBrand, x =>
    {
        //x.FilterBy(x => x.ID, StockPlusPlusActionTree.DataLevelAccess.ProductBrand)
        //.IncludeCreatedByCurrentUser(x => x.CreatedByUserID);
    }
    )
    {
        this.brandRepository = brandRepository;
        this.productCategoryRepository = productCategoryRepository;
    }
#else
    public ProductBrandController() : base(null)
    {

    }
#endif
#if (includeItemTemplateContent)
    /// <summary>
    /// Added as a sample code For Documentation Only
    /// </summary>
    /// <returns></returns>
    [NonAction]
    [AllowAnonymous]
    [HttpGet("test-insert-and-view")]
    public async Task<IActionResult> TestInsertAndView()
    {
        var createdBrand = await brandRepository.UpsertAsync(
            entity: new Data.Entities.ProductBrand(),
            dto: new ProductBrandDTO { Name = "One" },
            actionType: ShiftSoftware.ShiftEntity.Core.ActionTypes.Insert,
            userId: null
        );

        brandRepository.Add(createdBrand);

        await brandRepository.SaveChangesAsync();

        return Ok(await brandRepository.ViewAsync(createdBrand!));
    }

    /// <summary>
    /// Added as a sample code For Documentation Only
    /// </summary>
    /// <returns></returns>
    [NonAction]
    [AllowAnonymous]
    [HttpGet("test-find-and-update")]
    public async Task<IActionResult> TestFindAndUpdate()
    {
        var updatedBrand = await brandRepository.UpsertAsync(
            entity: await brandRepository.FindAsync(1),
            dto: new ProductBrandDTO { ID = "1", Name = "Updated" },
            actionType: ShiftSoftware.ShiftEntity.Core.ActionTypes.Update,
            userId: null
        );

        await brandRepository.SaveChangesAsync();

        return Ok(await brandRepository.ViewAsync(updatedBrand));
    }


    /// <summary>
    /// Added as a sample code For Documentation Only
    /// </summary>
    /// <returns></returns>
    [NonAction]
    [AllowAnonymous]
    [HttpGet("test-delete")]
    public async Task<IActionResult> TestDelete()
    {
        var deletedBrand = await brandRepository.DeleteAsync(
            entity: await brandRepository.FindAsync(1),
            isHardDelete: false,
            userId: null
        );

        await brandRepository.SaveChangesAsync();

        return Ok(await brandRepository.ViewAsync(deletedBrand));
    }
#endif
}