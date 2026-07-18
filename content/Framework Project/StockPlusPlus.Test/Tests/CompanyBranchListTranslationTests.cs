using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ShiftSoftware.ShiftIdentity.Core.DTOs.CompanyBranch;
using ShiftSoftware.ShiftIdentity.Data;
using ShiftSoftware.ShiftIdentity.Data.Repositories;

namespace StockPlusPlus.Test.Tests;

// REGRESSION: proves the CompanyBranch LIST projection survives being FILTERED — the exact composition the
// Team-form CompanyBranch <ShiftAutocomplete> triggers (GET api/IdentityCompanyBranch?$filter=CompanyId eq X) and
// the shape that originally threw "could not be translated". The list is served by the generated mapper; the fix
// is the ForList(CompanyId/CityId/RegionId) scope-id projections in CompanyBranchRepository — without them the
// filter target isn't projected, EF can't bind the Where, and it inlines the collection-bearing projection into
// the predicate. These tests go through the repo's real MapToList (generated mapper), then apply the filter and
// call ToQueryString() — which forces EF to translate to SQL WITHOUT a database connection, so it throws exactly
// if translation fails.
[Collection("API Collection")]
public class CompanyBranchListTranslationTests
{
    private readonly CustomWebApplicationFactory factory;

    public CompanyBranchListTranslationTests(CustomWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    // Dropdown OPEN: $filter=(CompanyId eq '<hash>')  →  .Where(dto.CompanyId == <decoded>)  on the projection.
    [Fact]
    public void CompanyBranchList_GeneratedMapper_WithCompanyIdFilter_Translates()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ShiftIdentityDbContext>();
        var repo = scope.ServiceProvider.GetRequiredService<CompanyBranchRepository>();

        var query = repo.MapToList(db.CompanyBranches)
            .Where(x => x.CompanyId == "1")                                            // client $filter (post-projection)
            .Where(x => !x.IsDeleted)                                                  // framework soft-delete
            .Where(x => x.TerminationDate == null && x.CompanyTerminationDate == null); // ApplyPostODataProcessing

        var sql = query.ToQueryString(); // throws InvalidOperationException("could not be translated") if it can't
        Assert.Contains("SELECT", sql, StringComparison.OrdinalIgnoreCase);
        // The scope-id ForList must reach the entity's CompanyID (not compare an unprojected/default value):
        Assert.Contains("[c].[CompanyID]", sql);
    }

    // Dropdown TYPING: adds (contains(tolower(Name),'x')) — a second scalar predicate on the same projection.
    [Fact]
    public void CompanyBranchList_GeneratedMapper_WithNameSearchAndCompanyIdFilter_Translates()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ShiftIdentityDbContext>();
        var repo = scope.ServiceProvider.GetRequiredService<CompanyBranchRepository>();

        var query = repo.MapToList(db.CompanyBranches)
            .Where(x => x.CompanyId == "1")
            .Where(x => x.Name != null && x.Name.ToLower().Contains("a"))
            .Where(x => !x.IsDeleted)
            .Where(x => x.TerminationDate == null && x.CompanyTerminationDate == null);

        var sql = query.ToQueryString();
        Assert.Contains("SELECT", sql, StringComparison.OrdinalIgnoreCase);
    }

    // The original crash was in CountAsync over the filtered projection — verify the COUNT tree translates too.
    [Fact]
    public void CompanyBranchList_GeneratedMapper_FilteredCount_Translates()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ShiftIdentityDbContext>();
        var repo = scope.ServiceProvider.GetRequiredService<CompanyBranchRepository>();

        var filtered = repo.MapToList(db.CompanyBranches)
            .Where(x => x.CompanyId == "1")
            .Where(x => !x.IsDeleted)
            .Where(x => x.TerminationDate == null && x.CompanyTerminationDate == null)
            .Select(x => 1);

        var sql = filtered.ToQueryString();
        Assert.Contains("SELECT", sql, StringComparison.OrdinalIgnoreCase);
    }
}
