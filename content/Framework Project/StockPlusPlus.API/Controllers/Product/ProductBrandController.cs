using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ShiftEntity.Web;
using ShiftSoftware.ShiftIdentity.Core.DTOs.UserGroup;
using StockPlusPlus.Data.Entities.Product;
using StockPlusPlus.Data.Repositories.Product;
using StockPlusPlus.Shared.ActionTrees;
using StockPlusPlus.Shared.DTOs.Product.ProductBrand;

namespace StockPlusPlus.API.Controllers.Product;

[Route("api/[controller]")]
public class ProductBrandController : ShiftEntitySecureControllerAsync<ProductBrandRepository, Data.Entities.Product.ProductBrand, ProductBrandListDTO, ProductBrandDTO>
{
    private readonly ProductBrandRepository brandRepository;
    private readonly ProductCategoryRepository productCategoryRepository;
    public ProductBrandController(ProductBrandRepository brandRepository, ProductCategoryRepository productCategoryRepository) : base(StockActionTrees.ProductBrand, x =>
    {
        x.FilterBy(x => x.ID, StockActionTrees.DataLevelAccess.ProductBrand)
        .IncludeCreatedByCurrentUser(x => x.CreatedByUserID);
    }
    )
    {
        this.brandRepository = brandRepository;
        this.productCategoryRepository = productCategoryRepository;
    }

    /// <summary>
    /// Added as a sample code For Documentation Only
    /// </summary>
    /// <returns></returns>
    [NonAction]
    [AllowAnonymous]
    [HttpGet("test-insert-and-view")]
    public async Task<IActionResult> TestInsertAndView()
    {
        var createdBrand = await this.brandRepository.UpsertAsync(
            entity: new ProductBrand(),
            dto: new ProductBrandDTO { Name = "One" },
            actionType: ShiftSoftware.ShiftEntity.Core.ActionTypes.Insert,
            userId: null
        );

        this.brandRepository.Add(createdBrand);

        await this.brandRepository.SaveChangesAsync();

        return Ok(await this.brandRepository.ViewAsync(createdBrand!));
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
        var updatedBrand = await this.brandRepository.UpsertAsync(
            entity: await this.brandRepository.FindAsync(1),
            dto: new ProductBrandDTO { ID = "1", Name = "Updated" },
            actionType: ShiftSoftware.ShiftEntity.Core.ActionTypes.Update,
            userId: null
        );

        await this.brandRepository.SaveChangesAsync();

        return Ok(await this.brandRepository.ViewAsync(updatedBrand));
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
        var deletedBrand = await this.brandRepository.DeleteAsync(
            entity: await this.brandRepository.FindAsync(1),
            isHardDelete: false,
            userId: null
        );

        await this.brandRepository.SaveChangesAsync();

        return Ok(await this.brandRepository.ViewAsync(deletedBrand));
    }
}
