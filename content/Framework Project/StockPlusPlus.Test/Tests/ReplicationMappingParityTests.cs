using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using ShiftSoftware.ShiftEntity.Model.Replication.IdentityModels;
using ShiftSoftware.ShiftIdentity.Data.Replication;
using StockPlusPlus.Test.Tests.Parity;
using Xunit;

namespace StockPlusPlus.Test.Tests;

/// <summary>
/// Correctness guard for the AutoMapper-free Cosmos replication mappings (ShiftIdentity.Data/Replication +
/// Dashboard.AspNetCore/Replication). Each fact runs the hand-written <c>ToXModel()</c> / <c>ApplyTo…()</c> and
/// asserts the produced document against a GOLDEN — the exact JSON the host's AutoMapper profile produced from
/// the same fixture, captured while AutoMapper was still in the container.
/// <para>
/// It used to assert the two implementations AGREED, resolving <c>IMapper</c> from the running host. That dies
/// at Step F1, when <c>AddShiftIdentityAutoMapper()</c> becomes <c>[Obsolete(error: true)]</c> and the oracle
/// disappears permanently. Worse, agreement was never the interesting property: nothing asserted that
/// <c>BranchID</c> survives an apply-onto, only that both arms treated it identically — and it is the Cosmos
/// partition key. A golden pins the absolute document.
/// </para>
/// <para>
/// Regenerate with <see cref="Parity.GoldenCapture"/> — but only while AutoMapper is still registered. After
/// F1 there is nothing left to capture, and a "regenerated" golden would just be the implementation under test
/// restating itself.
/// </para>
/// <para>
/// No host, no database: these are pure unit tests now. That also frees them from the API holding the build
/// lock, which is why they no longer carry <c>[Collection("API Collection")]</c>.
/// </para>
/// </summary>
public class ReplicationMappingParityTests
{
    private static readonly ReplicationFixtures F = new();

    // The SAME options the goldens were captured with. Do not "make this more realistic" — with no
    // IHashIdService in scope the hash-id converters short-circuit and IDs serialize raw. That was harmless
    // when both arms shared one serializer and cancelled out; against a frozen constant it does not cancel,
    // and changing it would shift all 24 goldens at once for a reason unrelated to mapping.
    private static readonly JsonSerializerOptions Opts = new() { WriteIndented = false };

    /// <summary>
    /// Compares by MEMBER PATH, not by string equality on two one-line JSON blobs. System.Text.Json's property
    /// order across <c>CompanyBranchSubItemModel : ReplicationModel : ShiftEntityViewAndUpsertDTO</c> is a
    /// resolver implementation detail: moving one property between a base and a derived class would fail every
    /// fact at once for a non-mapping reason, and a suite that fails that way gets deleted rather than debugged.
    /// </summary>
    private static void AssertGolden(object produced, string golden)
    {
        var expected = JsonNode.Parse(golden);
        var actual = JsonNode.Parse(JsonSerializer.Serialize(produced, Opts));

        var diffs = MemberPathDiff.CompareJson(expected, actual, produced.GetType().Name);

        Assert.True(diffs.Count == 0,
            $"{produced.GetType().Name} diverged from the golden in {diffs.Count} place(s):\n  " +
            string.Join("\n  ", diffs.Select(d => d.ToString())));
    }

    /// <summary>Runs an apply-onto against a POPULATED destination and returns it for comparison.</summary>
    private static CompanyBranchSubItemModel ApplyOnto(Action<CompanyBranchSubItemModel> apply)
    {
        var destination = F.ExistingSubItem();
        apply(destination);
        return destination;
    }

    private const string BrandGolden =
        "{\"Name\":\"Acme\",\"IntegrationId\":\"B-1\",\"BrandID\":77,\"id\":\"10\",\"CreateDate\":\"2026-01-02T03:04:05+00:00\",\"LastSaveDate\":\"2026-06-07T08:09:10+00:00\",\"CreatedByUserID\":\"111\",\"LastSavedByUserID\":\"222\",\"IsDeleted\":false}";

    private const string ServiceGolden =
        "{\"Name\":\"Wash\",\"IntegrationId\":\"S-1\",\"id\":\"20\",\"CreateDate\":\"2026-01-02T03:04:05+00:00\",\"LastSaveDate\":\"2026-06-07T08:09:10+00:00\",\"CreatedByUserID\":\"111\",\"LastSavedByUserID\":\"222\",\"IsDeleted\":false}";

    private const string DepartmentGolden =
        "{\"Name\":\"Sales\",\"IntegrationId\":\"D-1\",\"id\":\"30\",\"CreateDate\":\"2026-01-02T03:04:05+00:00\",\"LastSaveDate\":\"2026-06-07T08:09:10+00:00\",\"CreatedByUserID\":\"111\",\"LastSavedByUserID\":\"222\",\"IsDeleted\":false}";

    private const string CountryGolden =
        "{\"CountryID\":40,\"RegionID\":null,\"ItemType\":\"Country\",\"Name\":\"Iraq\",\"IntegrationId\":\"C-1\",\"ShortCode\":\"IQ\",\"CallingCode\":\"\\u002B964\",\"IsProtected\":true,\"Flag\":\"iq.png\",\"DisplayOrder\":3,\"id\":\"40\",\"CreateDate\":\"2026-01-02T03:04:05+00:00\",\"LastSaveDate\":\"2026-06-07T08:09:10+00:00\",\"CreatedByUserID\":\"111\",\"LastSavedByUserID\":\"222\",\"IsDeleted\":false}";

    private const string RegionGolden =
        "{\"CountryID\":40,\"RegionID\":50,\"Name\":\"KRG\",\"IntegrationId\":\"R-1\",\"ShortCode\":\"KRG\",\"IsProtected\":true,\"ItemType\":\"Region\",\"Flag\":\"krg.png\",\"DisplayOrder\":2,\"id\":\"50\",\"CreateDate\":\"2026-01-02T03:04:05+00:00\",\"LastSaveDate\":\"2026-06-07T08:09:10+00:00\",\"CreatedByUserID\":\"111\",\"LastSavedByUserID\":\"222\",\"IsDeleted\":false}";

    private const string RegionCityRegionGolden =
        "{\"CountryID\":\"40\",\"RegionID\":\"50\",\"Name\":\"KRG\",\"IntegrationId\":\"R-1\",\"ShortCode\":\"KRG\",\"IsProtected\":true,\"Flag\":\"krg.png\",\"DisplayOrder\":2,\"Country\":{\"CountryID\":40,\"RegionID\":null,\"ItemType\":\"Country\",\"Name\":\"Iraq\",\"IntegrationId\":\"C-1\",\"ShortCode\":\"IQ\",\"CallingCode\":\"\\u002B964\",\"IsProtected\":true,\"Flag\":\"iq.png\",\"DisplayOrder\":3,\"id\":\"40\",\"CreateDate\":\"2026-01-02T03:04:05+00:00\",\"LastSaveDate\":\"2026-06-07T08:09:10+00:00\",\"CreatedByUserID\":\"111\",\"LastSavedByUserID\":\"222\",\"IsDeleted\":false},\"id\":\"50\",\"CreateDate\":\"2026-01-02T03:04:05+00:00\",\"LastSaveDate\":\"2026-06-07T08:09:10+00:00\",\"CreatedByUserID\":\"111\",\"LastSavedByUserID\":\"222\",\"IsDeleted\":false}";

    private const string CityGolden =
        "{\"Name\":\"Erbil\",\"IntegrationId\":\"CT-1\",\"CountryID\":40,\"RegionID\":50,\"IsProtected\":true,\"ItemType\":\"City\",\"CityID\":9,\"DisplayOrder\":1,\"id\":\"60\",\"CreateDate\":\"2026-01-02T03:04:05+00:00\",\"LastSaveDate\":\"2026-06-07T08:09:10+00:00\",\"CreatedByUserID\":\"111\",\"LastSavedByUserID\":\"222\",\"IsDeleted\":false}";

    private const string CityCompanyBranchGolden =
        "{\"Name\":\"Erbil\",\"IntegrationId\":\"CT-1\",\"IsProtected\":true,\"DisplayOrder\":1,\"Region\":{\"CountryID\":\"40\",\"RegionID\":\"50\",\"Name\":\"KRG\",\"IntegrationId\":\"R-1\",\"ShortCode\":\"KRG\",\"IsProtected\":true,\"Flag\":\"krg.png\",\"DisplayOrder\":2,\"Country\":{\"CountryID\":40,\"RegionID\":null,\"ItemType\":\"Country\",\"Name\":\"Iraq\",\"IntegrationId\":\"C-1\",\"ShortCode\":\"IQ\",\"CallingCode\":\"\\u002B964\",\"IsProtected\":true,\"Flag\":\"iq.png\",\"DisplayOrder\":3,\"id\":\"40\",\"CreateDate\":\"2026-01-02T03:04:05+00:00\",\"LastSaveDate\":\"2026-06-07T08:09:10+00:00\",\"CreatedByUserID\":\"111\",\"LastSavedByUserID\":\"222\",\"IsDeleted\":false},\"id\":\"50\",\"CreateDate\":\"2026-01-02T03:04:05+00:00\",\"LastSaveDate\":\"2026-06-07T08:09:10+00:00\",\"CreatedByUserID\":\"111\",\"LastSavedByUserID\":\"222\",\"IsDeleted\":false},\"id\":\"60\",\"CreateDate\":\"2026-01-02T03:04:05+00:00\",\"LastSaveDate\":\"2026-06-07T08:09:10+00:00\",\"CreatedByUserID\":\"111\",\"LastSavedByUserID\":\"222\",\"IsDeleted\":false}";

    private const string CompanyGolden =
        "{\"Name\":\"Shift\",\"LegalName\":\"Shift LLC\",\"IntegrationId\":\"CO-1\",\"ShortCode\":\"SFT\",\"CompanyType\":0,\"Logo\":\"logo.png\",\"HQPhone\":\"123\",\"HQEmail\":\"hq@x.com\",\"HQAddress\":\"St 1\",\"Website\":\"x.com\",\"IsProtected\":true,\"TerminationDate\":null,\"CustomFields\":{},\"ParentCompanyID\":5,\"CompanyID\":70,\"DisplayOrder\":4,\"id\":\"70\",\"CreateDate\":\"2026-01-02T03:04:05+00:00\",\"LastSaveDate\":\"2026-06-07T08:09:10+00:00\",\"CreatedByUserID\":\"111\",\"LastSavedByUserID\":\"222\",\"IsDeleted\":false}";

    private const string CompanyBranchGolden =
        "{\"Name\":\"Main\",\"Phone\":\"555\",\"Phones\":[],\"ShortPhone\":\"5\",\"Email\":\"b@x.com\",\"Emails\":[],\"Address\":\"Addr\",\"IntegrationId\":\"BR-1\",\"ShortCode\":\"MB\",\"TerminationDate\":null,\"Location\":{\"Coordinates\":[44.1,36.2],\"Type\":\"Point\"},\"Photos\":\"p\",\"MobilePhotos\":\"mp\",\"WorkingHours\":\"9-5\",\"WorkingDays\":\"Mon-Fri\",\"IsProtected\":true,\"City\":{\"Name\":\"Erbil\",\"IntegrationId\":\"CT-1\",\"IsProtected\":true,\"DisplayOrder\":1,\"Region\":{\"CountryID\":\"40\",\"RegionID\":\"50\",\"Name\":\"KRG\",\"IntegrationId\":\"R-1\",\"ShortCode\":\"KRG\",\"IsProtected\":true,\"Flag\":\"krg.png\",\"DisplayOrder\":2,\"Country\":{\"CountryID\":40,\"RegionID\":null,\"ItemType\":\"Country\",\"Name\":\"Iraq\",\"IntegrationId\":\"C-1\",\"ShortCode\":\"IQ\",\"CallingCode\":\"\\u002B964\",\"IsProtected\":true,\"Flag\":\"iq.png\",\"DisplayOrder\":3,\"id\":\"40\",\"CreateDate\":\"2026-01-02T03:04:05+00:00\",\"LastSaveDate\":\"2026-06-07T08:09:10+00:00\",\"CreatedByUserID\":\"111\",\"LastSavedByUserID\":\"222\",\"IsDeleted\":false},\"id\":\"50\",\"CreateDate\":\"2026-01-02T03:04:05+00:00\",\"LastSaveDate\":\"2026-06-07T08:09:10+00:00\",\"CreatedByUserID\":\"111\",\"LastSavedByUserID\":\"222\",\"IsDeleted\":false},\"id\":\"60\",\"CreateDate\":\"2026-01-02T03:04:05+00:00\",\"LastSaveDate\":\"2026-06-07T08:09:10+00:00\",\"CreatedByUserID\":\"111\",\"LastSavedByUserID\":\"222\",\"IsDeleted\":false},\"Company\":{\"Name\":\"Shift\",\"LegalName\":\"Shift LLC\",\"IntegrationId\":\"CO-1\",\"ShortCode\":\"SFT\",\"CompanyType\":0,\"Logo\":\"logo.png\",\"HQPhone\":\"123\",\"HQEmail\":\"hq@x.com\",\"HQAddress\":\"St 1\",\"Website\":\"x.com\",\"IsProtected\":true,\"TerminationDate\":null,\"CustomFields\":{},\"ParentCompanyID\":5,\"CompanyID\":70,\"DisplayOrder\":4,\"id\":\"70\",\"CreateDate\":\"2026-01-02T03:04:05+00:00\",\"LastSaveDate\":\"2026-06-07T08:09:10+00:00\",\"CreatedByUserID\":\"111\",\"LastSavedByUserID\":\"222\",\"IsDeleted\":false},\"BranchID\":\"80\",\"ItemType\":\"Branch\",\"CustomFields\":{},\"RegionID\":50,\"CityID\":60,\"CompanyID\":70,\"CountryID\":40,\"CompanyBranchID\":88,\"DisplayOrder\":6,\"DisplayName\":\"Main Branch\",\"Description\":\"desc\",\"WebsiteURL\":null,\"PublishTargets\":[],\"id\":\"80\",\"CreateDate\":\"2026-01-02T03:04:05+00:00\",\"LastSaveDate\":\"2026-06-07T08:09:10+00:00\",\"CreatedByUserID\":\"111\",\"LastSavedByUserID\":\"222\",\"IsDeleted\":false}";

    private const string UserGolden =
        "{\"FullName\":\"Aza\",\"Username\":\"aza\",\"Phone\":\"999\",\"Email\":\"aza@x.com\",\"IntegrationId\":\"U-1\",\"IsProtected\":true,\"CompanyID\":70,\"CompanyBranchID\":88,\"RegionID\":50,\"CountryID\":40,\"id\":\"90\",\"CreateDate\":\"2026-01-02T03:04:05+00:00\",\"LastSaveDate\":\"2026-06-07T08:09:10+00:00\",\"CreatedByUserID\":\"111\",\"LastSavedByUserID\":\"222\",\"IsDeleted\":false}";

    private const string TeamGolden =
        "{\"Name\":\"Ops\",\"IntegrationId\":\"T-1\",\"Tags\":[\"a\",\"b\"],\"CompanyID\":70,\"TeamID\":5,\"CompanyBranches\":[{\"Name\":\"Main\",\"IntegrationId\":\"BR-1\",\"BranchID\":\"80\",\"ItemType\":\"Branch\",\"id\":\"300\",\"CreateDate\":\"2026-01-02T03:04:05+00:00\",\"LastSaveDate\":\"2026-06-07T08:09:10+00:00\",\"CreatedByUserID\":\"111\",\"LastSavedByUserID\":\"222\",\"IsDeleted\":false}],\"id\":\"200\",\"CreateDate\":\"2026-01-02T03:04:05+00:00\",\"LastSaveDate\":\"2026-06-07T08:09:10+00:00\",\"CreatedByUserID\":\"111\",\"LastSavedByUserID\":\"222\",\"IsDeleted\":false}";

    private const string CompanyBranchService_SubItemGolden =
        "{\"Name\":\"Wash\",\"IntegrationId\":\"S-1\",\"BranchID\":\"88\",\"ItemType\":\"Service\",\"id\":\"20\",\"CreateDate\":\"2026-01-02T03:04:05+00:00\",\"LastSaveDate\":\"2026-06-07T08:09:10+00:00\",\"CreatedByUserID\":\"111\",\"LastSavedByUserID\":\"222\",\"IsDeleted\":false}";

    private const string CompanyBranchDepartment_SubItemGolden =
        "{\"Name\":\"Sales\",\"IntegrationId\":\"D-1\",\"BranchID\":\"88\",\"ItemType\":\"Department\",\"id\":\"30\",\"CreateDate\":\"2026-01-02T03:04:05+00:00\",\"LastSaveDate\":\"2026-06-07T08:09:10+00:00\",\"CreatedByUserID\":\"111\",\"LastSavedByUserID\":\"222\",\"IsDeleted\":false}";

    private const string CompanyBranchBrand_SubItemGolden =
        "{\"Name\":\"Acme\",\"IntegrationId\":\"B-1\",\"BranchID\":\"88\",\"ItemType\":\"Brand\",\"id\":\"10\",\"CreateDate\":\"2026-01-02T03:04:05+00:00\",\"LastSaveDate\":\"2026-06-07T08:09:10+00:00\",\"CreatedByUserID\":\"111\",\"LastSavedByUserID\":\"222\",\"IsDeleted\":false}";

    private const string CompanyBranchService_NullNavGolden =
        "{\"Name\":null,\"IntegrationId\":null,\"BranchID\":\"88\",\"ItemType\":\"Service\",\"id\":\"20\",\"CreateDate\":\"2026-01-02T03:04:05+00:00\",\"LastSaveDate\":\"2026-06-07T08:09:10+00:00\",\"CreatedByUserID\":\"111\",\"LastSavedByUserID\":\"222\",\"IsDeleted\":false}";

    private const string CompanyBranchDepartment_NullNavGolden =
        "{\"Name\":null,\"IntegrationId\":null,\"BranchID\":\"88\",\"ItemType\":\"Department\",\"id\":\"30\",\"CreateDate\":\"2026-01-02T03:04:05+00:00\",\"LastSaveDate\":\"2026-06-07T08:09:10+00:00\",\"CreatedByUserID\":\"111\",\"LastSavedByUserID\":\"222\",\"IsDeleted\":false}";

    private const string CompanyBranchBrand_NullNavGolden =
        "{\"Name\":null,\"IntegrationId\":null,\"BranchID\":\"88\",\"ItemType\":\"Brand\",\"id\":\"10\",\"CreateDate\":\"2026-01-02T03:04:05+00:00\",\"LastSaveDate\":\"2026-06-07T08:09:10+00:00\",\"CreatedByUserID\":\"111\",\"LastSavedByUserID\":\"222\",\"IsDeleted\":false}";

    private const string Team_NullBranchNavGolden =
        "{\"Name\":\"Ops\",\"IntegrationId\":\"T-1\",\"Tags\":[],\"CompanyID\":70,\"TeamID\":5,\"CompanyBranches\":[{\"Name\":null,\"IntegrationId\":null,\"BranchID\":\"0\",\"ItemType\":\"Branch\",\"id\":\"300\",\"CreateDate\":\"2026-01-02T03:04:05+00:00\",\"LastSaveDate\":\"2026-06-07T08:09:10+00:00\",\"CreatedByUserID\":\"111\",\"LastSavedByUserID\":\"222\",\"IsDeleted\":false}],\"id\":\"200\",\"CreateDate\":\"2026-01-02T03:04:05+00:00\",\"LastSaveDate\":\"2026-06-07T08:09:10+00:00\",\"CreatedByUserID\":\"111\",\"LastSavedByUserID\":\"222\",\"IsDeleted\":false}";

    private const string Brand_ApplyToSubItemGolden =
        "{\"Name\":\"Acme\",\"IntegrationId\":\"B-1\",\"BranchID\":\"88\",\"ItemType\":\"Brand\",\"id\":\"10\",\"CreateDate\":\"2026-01-02T03:04:05+00:00\",\"LastSaveDate\":\"2026-06-07T08:09:10+00:00\",\"CreatedByUserID\":\"111\",\"LastSavedByUserID\":\"222\",\"IsDeleted\":false}";

    private const string Service_ApplyToSubItemGolden =
        "{\"Name\":\"Wash\",\"IntegrationId\":\"S-1\",\"BranchID\":\"88\",\"ItemType\":\"Service\",\"id\":\"20\",\"CreateDate\":\"2026-01-02T03:04:05+00:00\",\"LastSaveDate\":\"2026-06-07T08:09:10+00:00\",\"CreatedByUserID\":\"111\",\"LastSavedByUserID\":\"222\",\"IsDeleted\":false}";

    private const string Department_ApplyToSubItemGolden =
        "{\"Name\":\"Sales\",\"IntegrationId\":\"D-1\",\"BranchID\":\"88\",\"ItemType\":\"Department\",\"id\":\"30\",\"CreateDate\":\"2026-01-02T03:04:05+00:00\",\"LastSaveDate\":\"2026-06-07T08:09:10+00:00\",\"CreatedByUserID\":\"111\",\"LastSavedByUserID\":\"222\",\"IsDeleted\":false}";

    private const string Brand_DeletedGolden =
        "{\"Name\":\"Acme\",\"IntegrationId\":\"B-1\",\"BrandID\":77,\"id\":\"10\",\"CreateDate\":\"2026-01-02T03:04:05+00:00\",\"LastSaveDate\":\"2026-06-07T08:09:10+00:00\",\"CreatedByUserID\":\"111\",\"LastSavedByUserID\":\"222\",\"IsDeleted\":true}";

    private const string CompanyBranch_DeletedGolden =
        "{\"Name\":\"Main\",\"Phone\":\"555\",\"Phones\":[],\"ShortPhone\":\"5\",\"Email\":\"b@x.com\",\"Emails\":[],\"Address\":\"Addr\",\"IntegrationId\":\"BR-1\",\"ShortCode\":\"MB\",\"TerminationDate\":null,\"Location\":{\"Coordinates\":[44.1,36.2],\"Type\":\"Point\"},\"Photos\":\"p\",\"MobilePhotos\":\"mp\",\"WorkingHours\":\"9-5\",\"WorkingDays\":\"Mon-Fri\",\"IsProtected\":true,\"City\":{\"Name\":\"Erbil\",\"IntegrationId\":\"CT-1\",\"IsProtected\":true,\"DisplayOrder\":1,\"Region\":{\"CountryID\":\"40\",\"RegionID\":\"50\",\"Name\":\"KRG\",\"IntegrationId\":\"R-1\",\"ShortCode\":\"KRG\",\"IsProtected\":true,\"Flag\":\"krg.png\",\"DisplayOrder\":2,\"Country\":{\"CountryID\":40,\"RegionID\":null,\"ItemType\":\"Country\",\"Name\":\"Iraq\",\"IntegrationId\":\"C-1\",\"ShortCode\":\"IQ\",\"CallingCode\":\"\\u002B964\",\"IsProtected\":true,\"Flag\":\"iq.png\",\"DisplayOrder\":3,\"id\":\"40\",\"CreateDate\":\"2026-01-02T03:04:05+00:00\",\"LastSaveDate\":\"2026-06-07T08:09:10+00:00\",\"CreatedByUserID\":\"111\",\"LastSavedByUserID\":\"222\",\"IsDeleted\":false},\"id\":\"50\",\"CreateDate\":\"2026-01-02T03:04:05+00:00\",\"LastSaveDate\":\"2026-06-07T08:09:10+00:00\",\"CreatedByUserID\":\"111\",\"LastSavedByUserID\":\"222\",\"IsDeleted\":false},\"id\":\"60\",\"CreateDate\":\"2026-01-02T03:04:05+00:00\",\"LastSaveDate\":\"2026-06-07T08:09:10+00:00\",\"CreatedByUserID\":\"111\",\"LastSavedByUserID\":\"222\",\"IsDeleted\":false},\"Company\":{\"Name\":\"Shift\",\"LegalName\":\"Shift LLC\",\"IntegrationId\":\"CO-1\",\"ShortCode\":\"SFT\",\"CompanyType\":0,\"Logo\":\"logo.png\",\"HQPhone\":\"123\",\"HQEmail\":\"hq@x.com\",\"HQAddress\":\"St 1\",\"Website\":\"x.com\",\"IsProtected\":true,\"TerminationDate\":null,\"CustomFields\":{},\"ParentCompanyID\":5,\"CompanyID\":70,\"DisplayOrder\":4,\"id\":\"70\",\"CreateDate\":\"2026-01-02T03:04:05+00:00\",\"LastSaveDate\":\"2026-06-07T08:09:10+00:00\",\"CreatedByUserID\":\"111\",\"LastSavedByUserID\":\"222\",\"IsDeleted\":false},\"BranchID\":\"80\",\"ItemType\":\"Branch\",\"CustomFields\":{},\"RegionID\":50,\"CityID\":60,\"CompanyID\":70,\"CountryID\":40,\"CompanyBranchID\":88,\"DisplayOrder\":6,\"DisplayName\":\"Main Branch\",\"Description\":\"desc\",\"WebsiteURL\":null,\"PublishTargets\":[],\"id\":\"80\",\"CreateDate\":\"2026-01-02T03:04:05+00:00\",\"LastSaveDate\":\"2026-06-07T08:09:10+00:00\",\"CreatedByUserID\":\"111\",\"LastSavedByUserID\":\"222\",\"IsDeleted\":true}";

    [Fact] public void Brand() => AssertGolden(F.Brand().ToBrandModel(), BrandGolden);
    [Fact] public void Service() => AssertGolden(F.Service().ToServiceModel(), ServiceGolden);
    [Fact] public void Department() => AssertGolden(F.Department().ToDepartmentModel(), DepartmentGolden);
    [Fact] public void Country() => AssertGolden(F.Country().ToCountryModel(), CountryGolden);
    [Fact] public void Region() => AssertGolden(F.Region().ToRegionModel(), RegionGolden);
    [Fact] public void Region_AsCityRegion() => AssertGolden(F.Region().ToCityRegionModel(), RegionCityRegionGolden);
    [Fact] public void City() => AssertGolden(F.City().ToCityModel(), CityGolden);
    [Fact] public void City_AsCompanyBranch() => AssertGolden(F.City().ToCityCompanyBranchModel(), CityCompanyBranchGolden);
    [Fact] public void Company() => AssertGolden(F.Company().ToCompanyModel(), CompanyGolden);
    [Fact] public void CompanyBranch() => AssertGolden(F.CompanyBranch().ToCompanyBranchModel(), CompanyBranchGolden);
    [Fact] public void User() => AssertGolden(F.User().ToUserModel(), UserGolden);
    [Fact] public void Team() => AssertGolden(F.Team().ToTeamModel(), TeamGolden);
    [Fact] public void BranchService_AsSubItem() => AssertGolden(F.BranchService().ToCompanyBranchSubItemModel(), CompanyBranchService_SubItemGolden);
    [Fact] public void BranchDepartment_AsSubItem() => AssertGolden(F.BranchDepartment().ToCompanyBranchSubItemModel(), CompanyBranchDepartment_SubItemGolden);
    [Fact] public void BranchBrand_AsSubItem() => AssertGolden(F.BranchBrand().ToCompanyBranchSubItemModel(), CompanyBranchBrand_SubItemGolden);
    // ── The ACTUAL runtime case ───────────────────────────────────────────────────────────────────────────
    // The join row is inserted carrying only its FK, so the Service/Department/Brand navigation is null. The
    // mapping must null-propagate to a null name, not throw: a non-null-safe map NREs here and silently kills
    // replication, because the failure lands inside a swallowed per-row catch and the watermark still stamps
    // clean. These four goldens are the only build-enforced record of that behaviour once AutoMapper is gone —
    // note BranchID pinned as "0", which is IdentityReplicationMappingExtensions' deliberate
    // (src.CompanyBranch?.ID ?? 0).ToString(). A future reader who "fixes" that coalesce to null changes live
    // document content in a partitioned store; this is what stops them.

    [Fact] public void BranchService_NullNav() => AssertGolden(F.BranchService(withNav: false).ToCompanyBranchSubItemModel(), CompanyBranchService_NullNavGolden);
    [Fact] public void BranchDepartment_NullNav() => AssertGolden(F.BranchDepartment(withNav: false).ToCompanyBranchSubItemModel(), CompanyBranchDepartment_NullNavGolden);
    [Fact] public void BranchBrand_NullNav() => AssertGolden(F.BranchBrand(withNav: false).ToCompanyBranchSubItemModel(), CompanyBranchBrand_NullNavGolden);
    [Fact] public void Team_NullBranchNav() => AssertGolden(F.Team(withBranchNav: false).ToTeamModel(), Team_NullBranchNavGolden);
    // ── Apply ONTO a populated destination (the UpdateReference path) ────────────────────────────────────
    // The destination arrives with real values, which is the whole point: the question is which members get
    // overwritten and which survive. BranchID is the Cosmos PARTITION KEY and is deliberately never rewritten
    // — asserting the whole document is what pins that, where an equality check between two implementations
    // only ever proved they agreed.

    [Fact] public void Brand_AppliedOntoExistingSubItem() => AssertGolden(ApplyOnto(d => F.Brand().ApplyToCompanyBranchSubItem(d)), Brand_ApplyToSubItemGolden);
    [Fact] public void Service_AppliedOntoExistingSubItem() => AssertGolden(ApplyOnto(d => F.Service().ApplyToCompanyBranchSubItem(d)), Service_ApplyToSubItemGolden);
    [Fact] public void Department_AppliedOntoExistingSubItem() => AssertGolden(ApplyOnto(d => F.Department().ApplyToCompanyBranchSubItem(d)), Department_ApplyToSubItemGolden);
    // ── Tombstones ────────────────────────────────────────────────────────────────────────────────────────
    // Every other fixture is IsDeleted = false, so the deleted-row document — the entire reason replication
    // propagates the flag at all — had zero coverage across all 22 original facts.

    [Fact] public void Brand_Deleted() => AssertGolden(F.Brand(deleted: true).ToBrandModel(), Brand_DeletedGolden);
    [Fact] public void CompanyBranch_Deleted() => AssertGolden(F.CompanyBranch(deleted: true).ToCompanyBranchModel(), CompanyBranch_DeletedGolden);
}
