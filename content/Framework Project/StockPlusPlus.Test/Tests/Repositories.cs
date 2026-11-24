using Microsoft.Extensions.DependencyInjection;
using ShiftSoftware.ShiftFrameworkTestingTools;
using StockPlusPlus.Data.Entities;
using StockPlusPlus.Data.Repositories;

namespace StockPlusPlus.Test.Tests;

[TestCaseOrderer(typeof(PriorityOrderer))]
[Collection("API Collection")]
public class Repositories : IDisposable
{
    private readonly CustomWebApplicationFactory factory;
    private readonly ITestOutputHelper output;

    // The repositories come from a scope, as they do in a request. Resolved from the host's root provider, the context
    // behind them would live as long as the host, and a save handler it creates would have no scope to be created in.
    private readonly IServiceScope scope;

    private readonly ProductBrandRepository brandRepository;
    private readonly ProductCategoryRepository productCategoryRepository;
    private readonly ProductRepository productRepository;

    public Repositories(CustomWebApplicationFactory factory, ITestOutputHelper output)
    {
        this.factory = factory;
        this.output = output;

        this.scope = factory.Services.CreateScope();

        this.brandRepository = scope.ServiceProvider.GetRequiredService<ProductBrandRepository>();
        this.productCategoryRepository = scope.ServiceProvider.GetRequiredService<ProductCategoryRepository>();
        this.productRepository = scope.ServiceProvider.GetRequiredService<ProductRepository>();
    }

    public void Dispose() => scope.Dispose();

    [Fact]
    public async Task ProductRepository()
    {
        var brand = new ProductBrand { Name = "Brand One" };

        brandRepository.Add(brand);

        await brandRepository.SaveChangesAsync();

        var foundBrand = (await this.brandRepository.FindAsync(brand.ID, asOf: null, disableDefaultDataLevelAccess: true, disableGlobalFilters: true))!;

        var viewedBrand = await this.brandRepository.ViewAsync(foundBrand);

        Assert.NotNull(viewedBrand);

        Assert.Equal(brand.Name, foundBrand.Name);

        var productCategory = new ProductCategory { Name = "Product Category One" };

        productCategoryRepository.Add(productCategory);

        await productCategoryRepository.SaveChangesAsync();

        var product = new Product
        {
            Name = "Product One",
            ProductBrand = foundBrand,
            ProductCategory = productCategory
        };

        productRepository.Add(product);

        await productRepository.SaveChangesAsync();

        this.productRepository.db.ChangeTracker.Clear();

        var foundProduct = (await this.productRepository.FindAsync(product.ID, asOf: null, disableDefaultDataLevelAccess: true, disableGlobalFilters: true))!;

        var viewedProduct = await productRepository.ViewAsync(foundProduct);

        Assert.NotNull(viewedProduct);

        Assert.Equal(foundProduct.Name, viewedProduct.Name); 

        Assert.Equal(foundProduct.ProductBrand!.Name, brand.Name);

        //Product Repository does not include the product category
        Assert.Null(foundProduct.ProductCategory);

        Assert.Equal(foundProduct.ProductCategoryID, productCategory.ID);
    }
}