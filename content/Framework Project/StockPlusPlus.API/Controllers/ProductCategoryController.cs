using Microsoft.AspNetCore.Mvc;
using ShiftSoftware.ShiftEntity.Web;
using StockPlusPlus.Data.Repositories;
using StockPlusPlus.Shared.ActionTrees;
using StockPlusPlus.Shared.DTOs.ProductCategory;

namespace StockPlusPlus.API.Controllers;

public class Person
{
    public long Age { get; set; }
}

[Route("api/[controller]")]
public class ProductCategoryController : ShiftEntitySecureControllerAsync<ProductCategoryRepository, Data.Entities.ProductCategory, ProductCategoryListDTO, ProductCategoryDTO>
{
    private readonly ProductCategoryRepository repository;
    private readonly IConfiguration configuration;

    public ProductCategoryController(ProductCategoryRepository repository, IConfiguration configuration) : base(StockPlusPlusActionTree.ProductCategory, x =>
        x.FilterBy<List<Person>>(x => x.Value.Select(x => x.Age).Contains(x.Entity.ID) || x.WildCard)
        .CustomValueProvider((services, entity) => { 

            //You have access to IServiceProvider and the entity You should be able to do a lot of custom work to generate the list of values to return
            return [new() { Age = 31 }]; 
        })
    )
    {
        this.repository = repository;
        this.configuration = configuration;
    }
}