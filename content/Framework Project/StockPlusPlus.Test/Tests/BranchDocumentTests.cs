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
/// The branch's document in the <c>Branches</c> container, <see cref="BranchModel"/>, as
/// <see cref="ShiftIdentityReplicationMapper"/> builds it from a branch loaded with its replication tree. It carries
/// what the branch's <c>CompanyBranches</c> document carried, and lists the services, departments and brands the
/// branch offers: ordered by ID, without a link that is deleted and without a service, department or brand that is
/// deleted.
/// <para>
/// Same setup as <see cref="ReplicationMappingParityTests"/>: the package's own generated mapper through
/// <see cref="IMapper"/>, the same fixtures and serializer options, compared by member path. These goldens were
/// written for this document when it was introduced. Edit one only when the document is meant to change, and say why.
/// </para>
/// </summary>
public class BranchDocumentTests
{
    private static readonly ReplicationFixtures F = new();

    private static readonly IMapper M = Mapper.Create(typeof(ShiftIdentityReplicationMapper).Assembly);

    private static readonly JsonSerializerOptions Opts = new() { WriteIndented = false };

    private static void AssertGolden(object produced, string golden)
    {
        var diffs = MemberPathDiff.CompareJson(JsonNode.Parse(golden), JsonNode.Parse(JsonSerializer.Serialize(produced, Opts)), produced.GetType().Name);

        Assert.True(diffs.Count == 0,
            $"{produced.GetType().Name} diverged from the golden in {diffs.Count} place(s):\n  " +
            string.Join("\n  ", diffs.Select(d => d.ToString())));
    }

    private static T Item<T>(T item, long id, bool deleted = false) where T : ShiftEntity<T>
    {
        item.ID = id;
        item.IsDeleted = deleted;
        return item;
    }

    // The fixture branch with links listed out of ID order, so the document's order is its own. Each list has one
    // link that is deleted or whose item is deleted, and the services have one whose service was not loaded.
    private static CompanyBranch WithLinks(CompanyBranch branch)
    {
        branch.CompanyBranchServices = new List<CompanyBranchService>
        {
            new() { CompanyBranchID = 80, ServiceID = 25, Service = Item(new Service { Name = "Tint", IntegrationId = "S-5" }, 25) },
            new() { CompanyBranchID = 80, ServiceID = 23 },
            new() { CompanyBranchID = 80, ServiceID = 22, Service = Item(new Service { Name = "Polish", IntegrationId = "S-3" }, 22, deleted: true) },
            Item(new CompanyBranchService { CompanyBranchID = 80, ServiceID = 21, Service = Item(new Service { Name = "Paint", IntegrationId = "S-2" }, 21) }, 102, deleted: true),
            new() { CompanyBranchID = 80, ServiceID = 20, Service = F.Service() },
        };
        branch.CompanyBranchDepartments = new List<CompanyBranchDepartment>
        {
            new() { CompanyBranchID = 80, DepartmentID = 31, Department = Item(new Department { Name = "Support", IntegrationId = "D-2" }, 31, deleted: true) },
            new() { CompanyBranchID = 80, DepartmentID = 30, Department = F.Department() },
        };
        branch.CompanyBranchBrands = new List<CompanyBranchBrand>
        {
            Item(new CompanyBranchBrand { CompanyBranchID = 80, BrandID = 11, Brand = Item(new Brand { Name = "Other", IntegrationId = "B-2" }, 11) }, 103, deleted: true),
            new() { CompanyBranchID = 80, BrandID = 10, Brand = F.Brand() },
        };
        return branch;
    }

    [Fact]
    public void Branch_ListsWhatItOffers() => AssertGolden(M.Map<BranchModel>(WithLinks(F.CompanyBranch())), BranchGolden);

    [Fact]
    public void Branch_WithNothingLinked() => AssertGolden(M.Map<BranchModel>(F.CompanyBranch()), BranchWithNothingLinkedGolden);

    [Fact]
    public void Branch_WithItsLinksNotLoaded()
    {
        var branch = F.CompanyBranch();
        branch.CompanyBranchServices = null;
        branch.CompanyBranchDepartments = null;
        branch.CompanyBranchBrands = null;

        AssertGolden(M.Map<BranchModel>(branch), BranchWithNothingLinkedGolden);
    }

    // Everything but the three lists is the branch's CompanyBranches document, member for member: the frozen goldens of
    // that document, which nothing writes any more. So a reader that moved to Branches keeps its fields and its queries.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Branch_CarriesWhatItsCompanyBranchesDocumentCarries(bool deleted) =>
        ReplicationMappingParityTests.AssertGolden(M.Map<BranchModel>(WithLinks(F.CompanyBranch(deleted))),
            deleted ? ReplicationMappingParityTests.CompanyBranch_DeletedGolden : ReplicationMappingParityTests.CompanyBranchGolden,
            ReplicationMappingParityTests.BranchLists);

    private const string BranchGolden =
        "{\"Name\":\"Main\",\"Phone\":\"555\",\"Phones\":[],\"ShortPhone\":\"5\",\"Email\":\"b@x.com\",\"Emails\":[],\"Address\":\"Addr\",\"IntegrationId\":\"BR-1\",\"ShortCode\":\"MB\",\"TerminationDate\":null,\"Location\":{\"Coordinates\":[44.1,36.2],\"Type\":\"Point\"},\"Photos\":\"p\",\"MobilePhotos\":\"mp\",\"WorkingHours\":\"9-5\",\"WorkingDays\":\"Mon-Fri\",\"IsProtected\":true,\"City\":{\"Name\":\"Erbil\",\"IntegrationId\":\"CT-1\",\"IsProtected\":true,\"DisplayOrder\":1,\"Region\":{\"CountryID\":\"40\",\"RegionID\":\"50\",\"Name\":\"KRG\",\"IntegrationId\":\"R-1\",\"ShortCode\":\"KRG\",\"IsProtected\":true,\"Flag\":\"krg.png\",\"DisplayOrder\":2,\"Country\":{\"CountryID\":40,\"RegionID\":null,\"ItemType\":\"Country\",\"Name\":\"Iraq\",\"IntegrationId\":\"C-1\",\"ShortCode\":\"IQ\",\"CallingCode\":\"\\u002B964\",\"IsProtected\":true,\"Flag\":\"iq.png\",\"DisplayOrder\":3,\"id\":\"40\",\"CreateDate\":\"2026-01-02T03:04:05+00:00\",\"LastSaveDate\":\"2026-06-07T08:09:10+00:00\",\"CreatedByUserID\":\"111\",\"LastSavedByUserID\":\"222\",\"IsDeleted\":false},\"id\":\"50\",\"CreateDate\":\"2026-01-02T03:04:05+00:00\",\"LastSaveDate\":\"2026-06-07T08:09:10+00:00\",\"CreatedByUserID\":\"111\",\"LastSavedByUserID\":\"222\",\"IsDeleted\":false},\"id\":\"60\",\"CreateDate\":\"2026-01-02T03:04:05+00:00\",\"LastSaveDate\":\"2026-06-07T08:09:10+00:00\",\"CreatedByUserID\":\"111\",\"LastSavedByUserID\":\"222\",\"IsDeleted\":false},\"Company\":{\"Name\":\"Shift\",\"LegalName\":\"Shift LLC\",\"IntegrationId\":\"CO-1\",\"ShortCode\":\"SFT\",\"CompanyType\":0,\"Logo\":\"logo.png\",\"HQPhone\":\"123\",\"HQEmail\":\"hq@x.com\",\"HQAddress\":\"St 1\",\"Website\":\"x.com\",\"IsProtected\":true,\"TerminationDate\":null,\"CustomFields\":{},\"ParentCompanyID\":5,\"CompanyID\":70,\"DisplayOrder\":4,\"id\":\"70\",\"CreateDate\":\"2026-01-02T03:04:05+00:00\",\"LastSaveDate\":\"2026-06-07T08:09:10+00:00\",\"CreatedByUserID\":\"111\",\"LastSavedByUserID\":\"222\",\"IsDeleted\":false},\"BranchID\":\"80\",\"ItemType\":\"Branch\",\"CustomFields\":{},\"RegionID\":50,\"CityID\":60,\"CompanyID\":70,\"CountryID\":40,\"CompanyBranchID\":88,\"DisplayOrder\":6,\"DisplayName\":\"Main Branch\",\"Description\":\"desc\",\"WebsiteURL\":null,\"PublishTargets\":[],\"id\":\"80\",\"CreateDate\":\"2026-01-02T03:04:05+00:00\",\"LastSaveDate\":\"2026-06-07T08:09:10+00:00\",\"CreatedByUserID\":\"111\",\"LastSavedByUserID\":\"222\",\"IsDeleted\":false,\"Services\":[{\"ID\":20,\"IntegrationId\":\"S-1\",\"Name\":\"Wash\"},{\"ID\":25,\"IntegrationId\":\"S-5\",\"Name\":\"Tint\"}],\"Departments\":[{\"ID\":30,\"IntegrationId\":\"D-1\",\"Name\":\"Sales\"}],\"Brands\":[{\"ID\":10,\"IntegrationId\":\"B-1\",\"Name\":\"Acme\"}]}";

    private const string BranchWithNothingLinkedGolden =
        "{\"Name\":\"Main\",\"Phone\":\"555\",\"Phones\":[],\"ShortPhone\":\"5\",\"Email\":\"b@x.com\",\"Emails\":[],\"Address\":\"Addr\",\"IntegrationId\":\"BR-1\",\"ShortCode\":\"MB\",\"TerminationDate\":null,\"Location\":{\"Coordinates\":[44.1,36.2],\"Type\":\"Point\"},\"Photos\":\"p\",\"MobilePhotos\":\"mp\",\"WorkingHours\":\"9-5\",\"WorkingDays\":\"Mon-Fri\",\"IsProtected\":true,\"City\":{\"Name\":\"Erbil\",\"IntegrationId\":\"CT-1\",\"IsProtected\":true,\"DisplayOrder\":1,\"Region\":{\"CountryID\":\"40\",\"RegionID\":\"50\",\"Name\":\"KRG\",\"IntegrationId\":\"R-1\",\"ShortCode\":\"KRG\",\"IsProtected\":true,\"Flag\":\"krg.png\",\"DisplayOrder\":2,\"Country\":{\"CountryID\":40,\"RegionID\":null,\"ItemType\":\"Country\",\"Name\":\"Iraq\",\"IntegrationId\":\"C-1\",\"ShortCode\":\"IQ\",\"CallingCode\":\"\\u002B964\",\"IsProtected\":true,\"Flag\":\"iq.png\",\"DisplayOrder\":3,\"id\":\"40\",\"CreateDate\":\"2026-01-02T03:04:05+00:00\",\"LastSaveDate\":\"2026-06-07T08:09:10+00:00\",\"CreatedByUserID\":\"111\",\"LastSavedByUserID\":\"222\",\"IsDeleted\":false},\"id\":\"50\",\"CreateDate\":\"2026-01-02T03:04:05+00:00\",\"LastSaveDate\":\"2026-06-07T08:09:10+00:00\",\"CreatedByUserID\":\"111\",\"LastSavedByUserID\":\"222\",\"IsDeleted\":false},\"id\":\"60\",\"CreateDate\":\"2026-01-02T03:04:05+00:00\",\"LastSaveDate\":\"2026-06-07T08:09:10+00:00\",\"CreatedByUserID\":\"111\",\"LastSavedByUserID\":\"222\",\"IsDeleted\":false},\"Company\":{\"Name\":\"Shift\",\"LegalName\":\"Shift LLC\",\"IntegrationId\":\"CO-1\",\"ShortCode\":\"SFT\",\"CompanyType\":0,\"Logo\":\"logo.png\",\"HQPhone\":\"123\",\"HQEmail\":\"hq@x.com\",\"HQAddress\":\"St 1\",\"Website\":\"x.com\",\"IsProtected\":true,\"TerminationDate\":null,\"CustomFields\":{},\"ParentCompanyID\":5,\"CompanyID\":70,\"DisplayOrder\":4,\"id\":\"70\",\"CreateDate\":\"2026-01-02T03:04:05+00:00\",\"LastSaveDate\":\"2026-06-07T08:09:10+00:00\",\"CreatedByUserID\":\"111\",\"LastSavedByUserID\":\"222\",\"IsDeleted\":false},\"BranchID\":\"80\",\"ItemType\":\"Branch\",\"CustomFields\":{},\"RegionID\":50,\"CityID\":60,\"CompanyID\":70,\"CountryID\":40,\"CompanyBranchID\":88,\"DisplayOrder\":6,\"DisplayName\":\"Main Branch\",\"Description\":\"desc\",\"WebsiteURL\":null,\"PublishTargets\":[],\"id\":\"80\",\"CreateDate\":\"2026-01-02T03:04:05+00:00\",\"LastSaveDate\":\"2026-06-07T08:09:10+00:00\",\"CreatedByUserID\":\"111\",\"LastSavedByUserID\":\"222\",\"IsDeleted\":false,\"Services\":[],\"Departments\":[],\"Brands\":[]}";
}
