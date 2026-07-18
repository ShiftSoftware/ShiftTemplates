using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ShiftSoftware.ShiftIdentity.Core.DTOs.User;
using ShiftSoftware.ShiftIdentity.Data;
using ShiftSoftware.ShiftIdentity.Data.Repositories;

namespace StockPlusPlus.Test.Tests;

// REGRESSION (Phase 4): the User list DTO has the same risky shape as CompanyBranch — an AccessTrees collection
// projection plus the scope-ids CompanyBranchID/CompanyID (string←long?). Serving it via the generated mapper only
// stays translatable under a list filter because those scope-ids are ForList-projected (so EF can bind the Where
// and push it down instead of inlining the collection-bearing projection). ToQueryString() forces EF to translate
// to SQL without a DB connection, so it throws exactly if that breaks.
[Collection("API Collection")]
public class UserListTranslationTests
{
    private readonly CustomWebApplicationFactory factory;

    public UserListTranslationTests(CustomWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public void UserList_GeneratedMapper_WithScopeIdFilters_Translates()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ShiftIdentityDbContext>();
        var repo = scope.ServiceProvider.GetRequiredService<UserRepository>();

        var query = repo.MapToList(db.Users)
            .Where(x => x.CompanyBranchID == "1")
            .Where(x => x.CompanyID == "1")
            .Where(x => !x.IsDeleted);

        var sql = query.ToQueryString(); // throws InvalidOperationException("could not be translated") if it can't
        Assert.Contains("SELECT", sql, StringComparison.OrdinalIgnoreCase);
        // The scope-id ForLists must reach the entity's columns (not compare unprojected/default values):
        Assert.Contains("[u].[CompanyBranchID]", sql);
        Assert.Contains("[u].[CompanyID]", sql);
    }

    [Fact]
    public void UserList_GeneratedMapper_Materializes_WithCollectionAndLastSeen()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ShiftIdentityDbContext>();
        var repo = scope.ServiceProvider.GetRequiredService<UserRepository>();

        // Bare materialization (AccessTrees M:N projection + LastSeen UserLog-fallback coalesce).
        var sql = repo.MapToList(db.Users).ToQueryString();
        Assert.Contains("SELECT", sql, StringComparison.OrdinalIgnoreCase);
    }
}
