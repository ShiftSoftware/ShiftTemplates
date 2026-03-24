using Microsoft.AspNetCore.Mvc;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Web;
using StockPlusPlus.Data.Repositories;
using StockPlusPlus.Shared.ActionTrees;
using StockPlusPlus.Shared.DTOs.Product;

namespace StockPlusPlus.API.Controllers;

[Route("api/[controller]")]
public class ProductController(ProductRepository productRepository) :
    ShiftEntitySecureControllerAsync<ProductRepository, Data.Entities.Product, ProductListDTO, ProductDTO>(StockPlusPlusActionTree.Product)
{
    [HttpPost("bulk-delete")]
    public async Task<IActionResult> BulkDelete([FromBody] ShiftSoftware.ShiftEntity.Model.Dtos.SelectStateDTO<ProductListDTO> selectedItems)
    {
        productRepository.IncludeProductCategoryOnGetIquery = true;
        
        var items = await this.GetSelectedEntitiesAsync(selectedItems);

        try
        {
            await productRepository.BulkDeleteAsync(items);

            return Ok(new ShiftEntityResponse<IEnumerable<ProductListDTO>>()
            {
                Entity = items.Select(x => new ProductListDTO { })
            });
        }
        catch (ShiftEntityException ex)
        {
            return StatusCode(ex.HttpStatusCode, new ShiftEntityResponse<ProductListDTO>
            {
                Message = ex.Message,
                Additional = ex.AdditionalData,
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ShiftEntityResponse<ProductListDTO>
            {
                Message = new Message(ex.Message, ex.StackTrace ?? string.Empty)
            });
        }
    }
}