using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using ShiftMapper;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Model.Replication.IdentityModels;
using ShiftSoftware.ShiftIdentity.Data.Entities;
using ShiftSoftware.ShiftIdentity.Data.Replication;
using StockPlusPlus.Test.Tests.Parity;
using Xunit;

namespace StockPlusPlus.Test.Tests;

/// <summary>
/// Pins Identity's replication documents as the replication mapper writes them, for the cases the replication
/// aggregates plan keeps:
/// <list type="bullet">
/// <item>users, teams and the three authorization documents, which the plan leaves as they are;</item>
/// <item>a branch whose region has no country: its document in Branches carries a null country, as its document in
/// CompanyBranches did.</item>
/// </list>
/// The branch link documents of deleted links and deleted items were pinned here too, as the input to the plan's
/// decision about deleted items. Nothing writes those documents any more; <see cref="BranchDocumentTests"/> pins what
/// a branch's document lists instead. The frozen goldens in <see cref="ReplicationMappingParityTests"/> cover the other
/// documents. They are not touched here.
/// </summary>
public class IdentityDocumentPinTests
{
    private static readonly ReplicationFixtures F = new();
    private static readonly IMapper M = Mapper.Create(typeof(ShiftIdentityReplicationMapper).Assembly);

    // The options the frozen goldens use, so the documents compare the same way.
    private static readonly JsonSerializerOptions Opts = new() { WriteIndented = false };

    private static void Assert(object produced, string golden)
    {
        var diffs = MemberPathDiff.CompareJson(JsonNode.Parse(golden), JsonNode.Parse(JsonSerializer.Serialize(produced, Opts)),
            produced.GetType().Name);

        Xunit.Assert.True(diffs.Count == 0,
            $"{produced.GetType().Name} diverged from the golden in {diffs.Count} place(s):\n  " +
            string.Join("\n  ", diffs.Select(d => d.ToString())));
    }

    private static T Audited<T>(T entity, long id, bool deleted = false) where T : ShiftEntity<T>
    {
        entity.ID = id;
        entity.IsDeleted = deleted;
        entity.CreateDate = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
        entity.LastSaveDate = new DateTimeOffset(2026, 6, 7, 8, 9, 10, TimeSpan.Zero);
        entity.CreatedByUserID = 111;
        entity.LastSavedByUserID = 222;
        return entity;
    }

    private static AccessTree Tree() => Audited(new AccessTree { Name = "Shared", Tree = "{\"grant\":true}" }, 400);
    private static UserAccessTree Assignment() => Audited(new UserAccessTree { UserID = 90, AccessTreeID = 400 }, 401);
    private static TeamUser Membership() => Audited(new TeamUser { UserID = 90, TeamID = 200 }, 402);

    /// <summary>Every case, by name, with the document today's mapper produces for it.</summary>
    internal static IReadOnlyDictionary<string, Func<object>> Cases { get; } = new Dictionary<string, Func<object>>
    {
        ["User"] = () => M.Map<UserModel>(F.User()),
        ["User_Deleted"] = () => M.Map<UserModel>(F.User(deleted: true)),
        ["Team"] = () => M.Map<TeamModel>(F.Team()),
        ["AccessTree"] = () => M.Map<AccessTreeModel>(Tree()),
        ["UserAccessTree"] = () => M.Map<UserAccessTreeModel>(Assignment()),
        ["TeamUser"] = () => M.Map<TeamUserModel>(Membership()),

        ["CompanyBranch_RegionWithoutCountry"] = () =>
        {
            var branch = F.CompanyBranch();
            branch.City!.Region!.Country = null;
            return M.Map<BranchModel>(branch);
        },
    };

    [Fact]
    public void User_IsAsToday() => Assert(Cases["User"](), """
        {"FullName":"Aza","Username":"aza","Phone":"999","Email":"aza@x.com","IntegrationId":"U-1","IsActive":false,"AccessTree":null,"IsProtected":true,"CompanyID":70,"CompanyBranchID":88,"RegionID":50,"CountryID":40,"id":"90","CreateDate":"2026-01-02T03:04:05+00:00","LastSaveDate":"2026-06-07T08:09:10+00:00","CreatedByUserID":"111","LastSavedByUserID":"222","IsDeleted":false}
        """);

    [Fact]
    public void User_Deleted_IsAsToday() => Assert(Cases["User_Deleted"](), """
        {"FullName":"Aza","Username":"aza","Phone":"999","Email":"aza@x.com","IntegrationId":"U-1","IsActive":false,"AccessTree":null,"IsProtected":true,"CompanyID":70,"CompanyBranchID":88,"RegionID":50,"CountryID":40,"id":"90","CreateDate":"2026-01-02T03:04:05+00:00","LastSaveDate":"2026-06-07T08:09:10+00:00","CreatedByUserID":"111","LastSavedByUserID":"222","IsDeleted":true}
        """);

    [Fact]
    public void Team_IsAsToday() => Assert(Cases["Team"](), """
        {"Name":"Ops","IntegrationId":"T-1","Tags":["a","b"],"CompanyID":70,"TeamID":5,"CompanyBranches":[{"Name":"Main","IntegrationId":"BR-1","BranchID":"80","ItemType":"Branch","id":"300","CreateDate":"2026-01-02T03:04:05+00:00","LastSaveDate":"2026-06-07T08:09:10+00:00","CreatedByUserID":"111","LastSavedByUserID":"222","IsDeleted":false}],"id":"200","CreateDate":"2026-01-02T03:04:05+00:00","LastSaveDate":"2026-06-07T08:09:10+00:00","CreatedByUserID":"111","LastSavedByUserID":"222","IsDeleted":false}
        """);

    [Fact]
    public void AccessTree_IsAsToday() => Assert(Cases["AccessTree"](), """
        {"Name":"Shared","Tree":"{\u0022grant\u0022:true}","id":"400","CreateDate":"2026-01-02T03:04:05+00:00","LastSaveDate":"2026-06-07T08:09:10+00:00","CreatedByUserID":"111","LastSavedByUserID":"222","IsDeleted":false}
        """);

    [Fact]
    public void UserAccessTree_IsAsToday() => Assert(Cases["UserAccessTree"](), """
        {"UserID":90,"AccessTreeID":400,"id":"401","CreateDate":"2026-01-02T03:04:05+00:00","LastSaveDate":"2026-06-07T08:09:10+00:00","CreatedByUserID":"111","LastSavedByUserID":"222","IsDeleted":false}
        """);

    [Fact]
    public void TeamUser_IsAsToday() => Assert(Cases["TeamUser"](), """
        {"UserID":90,"TeamID":200,"id":"402","CreateDate":"2026-01-02T03:04:05+00:00","LastSaveDate":"2026-06-07T08:09:10+00:00","CreatedByUserID":"111","LastSavedByUserID":"222","IsDeleted":false}
        """);

    // Without the three lists BranchModel adds, which BranchDocumentTests pins.
    [Fact]
    public void CompanyBranch_RegionWithoutCountry_IsAsToday() => ReplicationMappingParityTests.AssertGolden(Cases["CompanyBranch_RegionWithoutCountry"](), """
        {"Name":"Main","Phone":"555","Phones":[],"ShortPhone":"5","Email":"b@x.com","Emails":[],"Address":"Addr","IntegrationId":"BR-1","ShortCode":"MB","TerminationDate":null,"Location":{"Coordinates":[44.1,36.2],"Type":"Point"},"Photos":"p","MobilePhotos":"mp","WorkingHours":"9-5","WorkingDays":"Mon-Fri","IsProtected":true,"City":{"Name":"Erbil","IntegrationId":"CT-1","IsProtected":true,"DisplayOrder":1,"Region":{"CountryID":"40","RegionID":"50","Name":"KRG","IntegrationId":"R-1","ShortCode":"KRG","IsProtected":true,"Flag":"krg.png","DisplayOrder":2,"Country":null,"id":"50","CreateDate":"2026-01-02T03:04:05+00:00","LastSaveDate":"2026-06-07T08:09:10+00:00","CreatedByUserID":"111","LastSavedByUserID":"222","IsDeleted":false},"id":"60","CreateDate":"2026-01-02T03:04:05+00:00","LastSaveDate":"2026-06-07T08:09:10+00:00","CreatedByUserID":"111","LastSavedByUserID":"222","IsDeleted":false},"Company":{"Name":"Shift","LegalName":"Shift LLC","IntegrationId":"CO-1","ShortCode":"SFT","CompanyType":0,"Logo":"logo.png","HQPhone":"123","HQEmail":"hq@x.com","HQAddress":"St 1","Website":"x.com","IsProtected":true,"TerminationDate":null,"CustomFields":{},"ParentCompanyID":5,"CompanyID":70,"DisplayOrder":4,"id":"70","CreateDate":"2026-01-02T03:04:05+00:00","LastSaveDate":"2026-06-07T08:09:10+00:00","CreatedByUserID":"111","LastSavedByUserID":"222","IsDeleted":false},"BranchID":"80","ItemType":"Branch","CustomFields":{},"RegionID":50,"CityID":60,"CompanyID":70,"CountryID":40,"CompanyBranchID":88,"DisplayOrder":6,"DisplayName":"Main Branch","Description":"desc","WebsiteURL":null,"PublishTargets":[],"id":"80","CreateDate":"2026-01-02T03:04:05+00:00","LastSaveDate":"2026-06-07T08:09:10+00:00","CreatedByUserID":"111","LastSavedByUserID":"222","IsDeleted":false}
        """, ReplicationMappingParityTests.BranchLists);
}
