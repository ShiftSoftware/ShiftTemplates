using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using ShiftSoftware.ShiftEntity.Web.Endpoints;
using ShiftSoftware.TypeAuth.AspNetCore.EndpointFilters;
using StockPlusPlus.Data.Repositories;
using StockPlusPlus.Shared.ActionTrees;
using StockPlusPlus.Shared.DTOs.Product;

namespace StockPlusPlus.API.Endpoints;

/// <summary>
/// Minimal-API surface for Product, serving the exact same URLs as the disabled
/// <see cref="Controllers.ProductController"/> (which is kept around with
/// <c>[NonController]</c> for reference but no longer registered by MVC).
///
/// Uses <c>MapShiftEntitySecureCrud</c> as a drop-in replacement for inheriting
/// <c>ShiftEntitySecureControllerAsync</c>, plus one hand-written endpoint
/// (<c>bulk-delete</c>) demonstrating the <c>RequireTypeAuthDelete</c> helper.
/// </summary>
public static class ProductEndpoints
{
    public static IEndpointRouteBuilder MapProductMinimalApi(this IEndpointRouteBuilder endpoints)
    {
        // Drop-in counterpart of ProductController's base class: same generics,
        // same TypeAuth action, same URL prefix as the (now disabled) controller.
        endpoints.MapShiftEntitySecureCrud<ProductRepository, Data.Entities.Product, ProductListDTO, ProductDTO>(
            "api/product",
            StockPlusPlusActionTree.Product);

        // Hand-written custom endpoint mirroring ProductController.BulkDelete,
        // showing the TypeAuth endpoint filter used directly.
        endpoints.MapPost("api/product/bulk-delete",
            async (HttpContext ctx, ProductRepository repo, SelectStateDTO<ProductListDTO> selectedItems) =>
            {
                repo.IncludeProductCategoryOnGetIquery = true;

                var handler = new ShiftSoftware.ShiftEntity.Web.ShiftEntityCrudHandler<
                    ProductRepository, Data.Entities.Product, ProductListDTO, ProductDTO>();

                var items = await handler.GetSelectedEntitiesAsync(ctx, selectedItems);

                try
                {
                    await repo.BulkDeleteAsync(items);

                    return Results.Ok(new ShiftEntityResponse<IEnumerable<ProductListDTO>>()
                    {
                        Entity = items.Select(x => new ProductListDTO { })
                    });
                }
                catch (ShiftEntityException ex)
                {
                    return Results.Json(new ShiftEntityResponse<ProductListDTO>
                    {
                        Message = ex.Message,
                        Additional = ex.AdditionalData,
                    }, statusCode: ex.HttpStatusCode);
                }
            })
            .RequireAuthorization()
            .RequireTypeAuthDelete(StockPlusPlusActionTree.Product);

        return endpoints;
    }
}
