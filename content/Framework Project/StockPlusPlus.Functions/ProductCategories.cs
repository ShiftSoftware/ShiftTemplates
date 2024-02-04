using Microsoft.AspNetCore.Authorization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using ShiftSoftware.TypeAuth.AspNetCore;
using ShiftSoftware.TypeAuth.Core;
using StockPlusPlus.Data.Repositories.Product;
using StockPlusPlus.Shared.ActionTrees;
using System.Net;

namespace StockPlusPlus.Functions
{
    public class ProductCategories
    {
        private readonly ProductCategoryRepository productCategoryRepository;
        public ProductCategories(ProductCategoryRepository productCategoryRepository)
        {
            this.productCategoryRepository = productCategoryRepository;
        }

        [Function("ProductCategories")]
        [TypeAuth(typeof(StockActionTrees), nameof(StockActionTrees.ProductCategory), Access.Read)]
        public async Task<HttpResponseData> Get(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequestData req)
        {
            var allProductCategories = await this.productCategoryRepository.OdataList().ToArrayAsync();

            Data.Entities.Product.ProductCategory? productCategory = null;

            if (allProductCategories.Count() > 0)
            { 
                var productCategoryId = long.Parse(allProductCategories.First().ID);
                productCategory = await this.productCategoryRepository.FindAsync(productCategoryId);
            }

            string responseMessage = System.Text.Json.JsonSerializer.Serialize(new
            {
                AllProducts = allProductCategories,
                FirstProductCategory = productCategory is null ? null : await this.productCategoryRepository.ViewAsync(productCategory)
            });

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                AllProducts = allProductCategories,
                FirstProductCategory = productCategory is null ? null : await this.productCategoryRepository.ViewAsync(productCategory)
            });

            return response;
        }
    }
}
