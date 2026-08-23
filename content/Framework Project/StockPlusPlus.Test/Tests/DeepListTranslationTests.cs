using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ShiftSoftware.ShiftEntity.EFCore;
using ShiftSoftware.ShiftIdentity.Data;
using ShiftSoftware.ShiftIdentity.Data.Repositories;
using StockPlusPlus.Data.DbContext;
using StockPlusPlus.Data.Entities;
using StockPlusPlus.Data.Repositories;
using StockPlusPlus.Shared.DTOs.Invoice;
using Xunit;

namespace StockPlusPlus.Test.Tests;

/// <summary>
/// SQL-translation coverage for every DEEP list projection — a list DTO that composes a child collection or a
/// child object.
/// <para>
/// This is not a mapping test. The entire OData pipeline — <c>$filter</c>, the soft-delete filter,
/// <c>ApplyPostODataProcessing</c>, <c>$orderby</c>, <c>$skip</c>/<c>$top</c> — is applied to the
/// <b>already-projected</b> queryable. So a collection-bearing member-init that materialises fine in a unit
/// test can become untranslatable the first time a user types in a filter box: the page works all through
/// testing and 500s in production on a column header click. <c>ToQueryString()</c> forces EF to render T-SQL
/// without opening a connection, so it throws exactly when translation breaks.
/// </para>
/// <para>
/// <b>Soft delete is deliberately NOT asserted inside composed children.</b> Steps A9 and B10 were both
/// dropped: filtering deleted rows is the repository and OData layer's job, mapping does not do it, and
/// AutoMapper never did either. The assertion here is the inverse — exactly ONE <c>IsDeleted</c> predicate, at
/// the root — which pins that composed children are unfiltered on purpose. Same rule as
/// <c>TaggingTests.Product_DeletedTag_IsStillReturnedOnBothTheViewAndTheList</c>.
/// </para>
/// <para>
/// Assertions name TABLES, never EF's aliases (<c>[i0]</c>, <c>[c2]</c>). Aliases renumber on any EF minor
/// bump, which turns a green suite red for no reason anyone can act on.
/// </para>
/// </summary>
[Collection("API Collection")]
public class DeepListTranslationTests
{
    private readonly CustomWebApplicationFactory factory;

    public DeepListTranslationTests(CustomWebApplicationFactory factory) => this.factory = factory;

    private static int Occurrences(string sql, string pattern) => Regex.Matches(sql, pattern).Count;

    private const string RootSoftDelete = @"\[IsDeleted\] = CAST\(0 AS bit\)";

    /// <summary>
    /// <c>api/invoice-deep</c> — Invoice → InvoiceLines → Product → ProductBrand, composed AUTOMATICALLY from a
    /// bare <c>UseGeneratedMapper()</c>. Three levels, zero configuration, the whole member-init baked into
    /// <c>__shiftListProjection</c>.
    /// </summary>
    [Fact]
    public void InvoiceDeepList_AutoDeepThreeLevels_Translates()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DB>();
        var repo = scope.ServiceProvider
            .GetRequiredService<ShiftRepository<DB, Invoice, InvoiceDeepListDTO, InvoiceDeepDTO>>();

        var sql = repo.MapToList(db.Invoices.AsNoTracking())
            .Where(x => !x.IsDeleted)                        // ApplyDefaultSoftDeleteFilter
            .OrderBy(x => x.ID)                              // $orderby + EnsureStableOrdering
            .Skip(0).Take(10)                                // $skip / $top
            .ToQueryString();

        Assert.Contains("[InvoiceLines]", sql);              // depth 1 reaches SQL
        Assert.Contains("[Products]", sql);                  // depth 2
        Assert.Contains("[ProductBrandID]", sql);            // depth 3

        // Root only. A composed child carries no soft-delete predicate, by design.
        Assert.Equal(1, Occurrences(sql, RootSoftDelete));
    }

    /// <summary>
    /// <c>api/invoice</c> — the EXPLICIT per-level form: <c>ForListChildren(…, line =&gt; line.ForChild(…))</c>.
    /// These children are not in the baked projection; they are merged at runtime by <c>ComposeList</c>, so
    /// this must go through the real repository rather than a registry-resolved mapper.
    /// </summary>
    [Fact]
    public void InvoiceList_ExplicitForListChildren_Translates()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DB>();
        var repo = scope.ServiceProvider.GetRequiredService<InvoiceRepository>();

        var sql = repo.MapToList(db.Invoices.AsNoTracking())
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.ID)
            .Skip(0).Take(10)
            .ToQueryString();

        Assert.Contains("[InvoiceLines]", sql);
        Assert.Contains("[Products]", sql);
        Assert.Equal(1, Occurrences(sql, RootSoftDelete));
    }

    /// <summary>
    /// <c>api/IdentityCompany</c> — <c>Brands</c> is a two-hop aggregation across the branch M:N
    /// (<c>CompanyBranches.SelectMany(CompanyBranchBrands)</c>, distinct). The riskiest list shape in
    /// ShiftIdentity and, until now, the only deep one with no translation coverage.
    /// </summary>
    [Fact]
    public void CompanyList_BrandsAggregation_Translates()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ShiftIdentityDbContext>();
        var repo = scope.ServiceProvider.GetRequiredService<CompanyRepository>();

        var sql = repo.MapToList(db.Companies)
            .Where(x => x.ParentCompanyID == "1")            // $filter on a CONVENTION-baked scope id — the
                                                             // ForList for this member was removed once the
                                                             // list convention covered long?→string.
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.Name).ThenBy(x => x.ID)
            .ToQueryString();

        Assert.Contains("[CompanyBranchBrands]", sql);
        Assert.Equal(1, Occurrences(sql, RootSoftDelete));
    }

    /// <summary>
    /// The Product list — <c>Tags</c> is spliced into the member-init by <c>SelectWithTags</c>. Not a generated
    /// deep list, but the identical risk: a collection binding that the OData pipeline then filters and orders
    /// on top of. It is also the one deep list a real user drives with a filter panel (ProductList.razor +
    /// ShiftTagFilter), which makes it the likeliest to break in production.
    /// </summary>
    [Fact]
    public void ProductList_TaggableProjection_Translates()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DB>();
        var repo = scope.ServiceProvider.GetRequiredService<ProductRepository>();

        var sql = repo.MapToList(db.Products.AsNoTracking())
            .Where(x => x.Name.ToLower().Contains("a"))
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.Name).ThenBy(x => x.ID)
            .ToQueryString();

        Assert.Contains("[Tags]", sql);

        // A retired tag stays visible on the rows already carrying it — the same rule as composed children.
        Assert.Equal(1, Occurrences(sql, RootSoftDelete));
    }
}
