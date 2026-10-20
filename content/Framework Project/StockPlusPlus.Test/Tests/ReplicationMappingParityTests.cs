using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using ShiftMapper;
using ShiftSoftware.ShiftEntity.Model.Replication.IdentityModels;
using ShiftSoftware.ShiftIdentity.Data.Replication;
using StockPlusPlus.Test.Tests.Parity;
using Xunit;

namespace StockPlusPlus.Test.Tests;

/// <summary>
/// Correctness guard for the Cosmos replication mappings of the ShiftIdentity domain — the pairs
/// <see cref="ShiftIdentityReplicationMapper"/> (ShiftIdentity.Data/Replication) declares, which both the save trigger
/// (Dashboard.AspNetCore/Replication) and the catch-up sweep (AzureFunctions/Replication) map through. Each fact maps
/// a fixture through that mapper — <c>Map&lt;XModel&gt;(entity)</c> — and asserts
/// the produced document against a GOLDEN: the exact JSON the host's AutoMapper profile produced from the same
/// fixture, captured while AutoMapper was still in the container, and matched byte-for-byte by the hand-written
/// delegates that came between. The ShiftMapper mapper is the third implementation these constants have pinned.
/// <para>
/// It used to assert the two implementations AGREED, resolving <c>IMapper</c> from the running host. That
/// died when <c>AddShiftIdentityAutoMapper()</c> was deleted — it was never obsoleted first — and the oracle
/// disappeared with it. Worse, agreement was never the interesting property: it showed that both arms treated a
/// document alike, not what the document is. A golden pins the absolute document.
/// </para>
/// <para>
/// The branch's goldens are its document in <c>CompanyBranches</c>, which nothing writes any more. They now pin the
/// branch's document in <c>Branches</c>, <see cref="BranchModel"/>, without its three lists, which
/// <see cref="BranchDocumentTests"/> pins: a reader that moved from one container to the other reads the same members.
/// </para>
/// <para>
/// The goldens are FROZEN, and there is no capture harness any more: the oracle that produced them no longer
/// exists in any assembly, so nothing can regenerate them. A "regenerated" golden would just be the
/// implementation under test restating itself. Edit a constant below only when the document is MEANT to
/// change, and say why.
/// </para>
/// <para>
/// No host, no database: these are pure unit tests. The mapper is built outside DI with <see cref="Mapper.Create"/>
/// over ShiftIdentity.Data — the assembly's own GENERATED mapper, which is legal because the mapper class takes no
/// dependencies — so they are also free of the API holding the build lock, which is why they carry no
/// <c>[Collection("API Collection")]</c>. It is held as <see cref="IMapper"/>, the run-time door, on purpose: that is
/// the door the replication pipeline resolves (it cannot name the host's types), and the typed methods the generator
/// writes onto <see cref="Mapper"/> are extension methods of whichever assembly runs the generator — this one does
/// not, and giving it one would pin THIS assembly's re-baked maps rather than the package's own.
/// </para>
/// </summary>
public class ReplicationMappingParityTests
{
    private static readonly ReplicationFixtures F = new();

    // One instance for the class: the mapper is stateless apart from its compiled-customization cache, and
    // building it per fact would only re-run the mapper class's constructor 15 times.
    private static readonly IMapper M = Mapper.Create(typeof(ShiftIdentityReplicationMapper).Assembly);

    // The SAME options the goldens were captured with. Do not "make this more realistic" — with no
    // IHashIdService in scope the hash-id converters short-circuit and IDs serialize raw. That was harmless
    // when both arms shared one serializer and cancelled out; against a frozen constant it does not cancel,
    // and changing it would shift all 15 goldens at once for a reason unrelated to mapping.
    private static readonly JsonSerializerOptions Opts = new() { WriteIndented = false };

    // The members BranchModel adds to the branch's CompanyBranches document.
    internal static readonly string[] BranchLists = ["Services", "Departments", "Brands"];

    /// <summary>
    /// Compares by MEMBER PATH, not by string equality on two one-line JSON blobs. System.Text.Json's property
    /// order across <c>CompanyBranchSubItemModel : ReplicationModel : ShiftEntityViewAndUpsertDTO</c> is a
    /// resolver implementation detail: moving one property between a base and a derived class would fail every
    /// fact at once for a non-mapping reason, and a suite that fails that way gets deleted rather than debugged.
    /// The members named in <paramref name="leftOut"/> are taken out of the produced document first.
    /// </summary>
    internal static void AssertGolden(object produced, string golden, params string[] leftOut)
    {
        var expected = JsonNode.Parse(golden);
        var actual = JsonNode.Parse(JsonSerializer.Serialize(produced, Opts))!;
        foreach (var member in leftOut)
            Assert.True(actual.AsObject().Remove(member), $"{produced.GetType().Name} has no {member}.");

        var diffs = MemberPathDiff.CompareJson(expected, actual, produced.GetType().Name);

        Assert.True(diffs.Count == 0,
            $"{produced.GetType().Name} diverged from the golden in {diffs.Count} place(s):\n  " +
            string.Join("\n  ", diffs.Select(d => d.ToString())));
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

    internal const string CompanyBranchGolden =
        "{\"Name\":\"Main\",\"Phone\":\"555\",\"Phones\":[],\"ShortPhone\":\"5\",\"Email\":\"b@x.com\",\"Emails\":[],\"Address\":\"Addr\",\"IntegrationId\":\"BR-1\",\"ShortCode\":\"MB\",\"TerminationDate\":null,\"Location\":{\"Coordinates\":[44.1,36.2],\"Type\":\"Point\"},\"Photos\":\"p\",\"MobilePhotos\":\"mp\",\"WorkingHours\":\"9-5\",\"WorkingDays\":\"Mon-Fri\",\"IsProtected\":true,\"City\":{\"Name\":\"Erbil\",\"IntegrationId\":\"CT-1\",\"IsProtected\":true,\"DisplayOrder\":1,\"Region\":{\"CountryID\":\"40\",\"RegionID\":\"50\",\"Name\":\"KRG\",\"IntegrationId\":\"R-1\",\"ShortCode\":\"KRG\",\"IsProtected\":true,\"Flag\":\"krg.png\",\"DisplayOrder\":2,\"Country\":{\"CountryID\":40,\"RegionID\":null,\"ItemType\":\"Country\",\"Name\":\"Iraq\",\"IntegrationId\":\"C-1\",\"ShortCode\":\"IQ\",\"CallingCode\":\"\\u002B964\",\"IsProtected\":true,\"Flag\":\"iq.png\",\"DisplayOrder\":3,\"id\":\"40\",\"CreateDate\":\"2026-01-02T03:04:05+00:00\",\"LastSaveDate\":\"2026-06-07T08:09:10+00:00\",\"CreatedByUserID\":\"111\",\"LastSavedByUserID\":\"222\",\"IsDeleted\":false},\"id\":\"50\",\"CreateDate\":\"2026-01-02T03:04:05+00:00\",\"LastSaveDate\":\"2026-06-07T08:09:10+00:00\",\"CreatedByUserID\":\"111\",\"LastSavedByUserID\":\"222\",\"IsDeleted\":false},\"id\":\"60\",\"CreateDate\":\"2026-01-02T03:04:05+00:00\",\"LastSaveDate\":\"2026-06-07T08:09:10+00:00\",\"CreatedByUserID\":\"111\",\"LastSavedByUserID\":\"222\",\"IsDeleted\":false},\"Company\":{\"Name\":\"Shift\",\"LegalName\":\"Shift LLC\",\"IntegrationId\":\"CO-1\",\"ShortCode\":\"SFT\",\"CompanyType\":0,\"Logo\":\"logo.png\",\"HQPhone\":\"123\",\"HQEmail\":\"hq@x.com\",\"HQAddress\":\"St 1\",\"Website\":\"x.com\",\"IsProtected\":true,\"TerminationDate\":null,\"CustomFields\":{},\"ParentCompanyID\":5,\"CompanyID\":70,\"DisplayOrder\":4,\"id\":\"70\",\"CreateDate\":\"2026-01-02T03:04:05+00:00\",\"LastSaveDate\":\"2026-06-07T08:09:10+00:00\",\"CreatedByUserID\":\"111\",\"LastSavedByUserID\":\"222\",\"IsDeleted\":false},\"BranchID\":\"80\",\"ItemType\":\"Branch\",\"CustomFields\":{},\"RegionID\":50,\"CityID\":60,\"CompanyID\":70,\"CountryID\":40,\"CompanyBranchID\":88,\"DisplayOrder\":6,\"DisplayName\":\"Main Branch\",\"Description\":\"desc\",\"WebsiteURL\":null,\"PublishTargets\":[],\"id\":\"80\",\"CreateDate\":\"2026-01-02T03:04:05+00:00\",\"LastSaveDate\":\"2026-06-07T08:09:10+00:00\",\"CreatedByUserID\":\"111\",\"LastSavedByUserID\":\"222\",\"IsDeleted\":false}";

    private const string UserGolden =
        "{\"FullName\":\"Aza\",\"Username\":\"aza\",\"Phone\":\"999\",\"Email\":\"aza@x.com\",\"IntegrationId\":\"U-1\",\"IsProtected\":true,\"CompanyID\":70,\"CompanyBranchID\":88,\"RegionID\":50,\"CountryID\":40,\"id\":\"90\",\"CreateDate\":\"2026-01-02T03:04:05+00:00\",\"LastSaveDate\":\"2026-06-07T08:09:10+00:00\",\"CreatedByUserID\":\"111\",\"LastSavedByUserID\":\"222\",\"IsDeleted\":false}";

    private const string TeamGolden =
        "{\"Name\":\"Ops\",\"IntegrationId\":\"T-1\",\"Tags\":[\"a\",\"b\"],\"CompanyID\":70,\"TeamID\":5,\"CompanyBranches\":[{\"Name\":\"Main\",\"IntegrationId\":\"BR-1\",\"BranchID\":\"80\",\"ItemType\":\"Branch\",\"id\":\"300\",\"CreateDate\":\"2026-01-02T03:04:05+00:00\",\"LastSaveDate\":\"2026-06-07T08:09:10+00:00\",\"CreatedByUserID\":\"111\",\"LastSavedByUserID\":\"222\",\"IsDeleted\":false}],\"id\":\"200\",\"CreateDate\":\"2026-01-02T03:04:05+00:00\",\"LastSaveDate\":\"2026-06-07T08:09:10+00:00\",\"CreatedByUserID\":\"111\",\"LastSavedByUserID\":\"222\",\"IsDeleted\":false}";

    private const string Team_NullBranchNavGolden =
        "{\"Name\":\"Ops\",\"IntegrationId\":\"T-1\",\"Tags\":[],\"CompanyID\":70,\"TeamID\":5,\"CompanyBranches\":[{\"Name\":null,\"IntegrationId\":null,\"BranchID\":\"0\",\"ItemType\":\"Branch\",\"id\":\"300\",\"CreateDate\":\"2026-01-02T03:04:05+00:00\",\"LastSaveDate\":\"2026-06-07T08:09:10+00:00\",\"CreatedByUserID\":\"111\",\"LastSavedByUserID\":\"222\",\"IsDeleted\":false}],\"id\":\"200\",\"CreateDate\":\"2026-01-02T03:04:05+00:00\",\"LastSaveDate\":\"2026-06-07T08:09:10+00:00\",\"CreatedByUserID\":\"111\",\"LastSavedByUserID\":\"222\",\"IsDeleted\":false}";

    private const string Brand_DeletedGolden =
        "{\"Name\":\"Acme\",\"IntegrationId\":\"B-1\",\"BrandID\":77,\"id\":\"10\",\"CreateDate\":\"2026-01-02T03:04:05+00:00\",\"LastSaveDate\":\"2026-06-07T08:09:10+00:00\",\"CreatedByUserID\":\"111\",\"LastSavedByUserID\":\"222\",\"IsDeleted\":true}";

    internal const string CompanyBranch_DeletedGolden =
        "{\"Name\":\"Main\",\"Phone\":\"555\",\"Phones\":[],\"ShortPhone\":\"5\",\"Email\":\"b@x.com\",\"Emails\":[],\"Address\":\"Addr\",\"IntegrationId\":\"BR-1\",\"ShortCode\":\"MB\",\"TerminationDate\":null,\"Location\":{\"Coordinates\":[44.1,36.2],\"Type\":\"Point\"},\"Photos\":\"p\",\"MobilePhotos\":\"mp\",\"WorkingHours\":\"9-5\",\"WorkingDays\":\"Mon-Fri\",\"IsProtected\":true,\"City\":{\"Name\":\"Erbil\",\"IntegrationId\":\"CT-1\",\"IsProtected\":true,\"DisplayOrder\":1,\"Region\":{\"CountryID\":\"40\",\"RegionID\":\"50\",\"Name\":\"KRG\",\"IntegrationId\":\"R-1\",\"ShortCode\":\"KRG\",\"IsProtected\":true,\"Flag\":\"krg.png\",\"DisplayOrder\":2,\"Country\":{\"CountryID\":40,\"RegionID\":null,\"ItemType\":\"Country\",\"Name\":\"Iraq\",\"IntegrationId\":\"C-1\",\"ShortCode\":\"IQ\",\"CallingCode\":\"\\u002B964\",\"IsProtected\":true,\"Flag\":\"iq.png\",\"DisplayOrder\":3,\"id\":\"40\",\"CreateDate\":\"2026-01-02T03:04:05+00:00\",\"LastSaveDate\":\"2026-06-07T08:09:10+00:00\",\"CreatedByUserID\":\"111\",\"LastSavedByUserID\":\"222\",\"IsDeleted\":false},\"id\":\"50\",\"CreateDate\":\"2026-01-02T03:04:05+00:00\",\"LastSaveDate\":\"2026-06-07T08:09:10+00:00\",\"CreatedByUserID\":\"111\",\"LastSavedByUserID\":\"222\",\"IsDeleted\":false},\"id\":\"60\",\"CreateDate\":\"2026-01-02T03:04:05+00:00\",\"LastSaveDate\":\"2026-06-07T08:09:10+00:00\",\"CreatedByUserID\":\"111\",\"LastSavedByUserID\":\"222\",\"IsDeleted\":false},\"Company\":{\"Name\":\"Shift\",\"LegalName\":\"Shift LLC\",\"IntegrationId\":\"CO-1\",\"ShortCode\":\"SFT\",\"CompanyType\":0,\"Logo\":\"logo.png\",\"HQPhone\":\"123\",\"HQEmail\":\"hq@x.com\",\"HQAddress\":\"St 1\",\"Website\":\"x.com\",\"IsProtected\":true,\"TerminationDate\":null,\"CustomFields\":{},\"ParentCompanyID\":5,\"CompanyID\":70,\"DisplayOrder\":4,\"id\":\"70\",\"CreateDate\":\"2026-01-02T03:04:05+00:00\",\"LastSaveDate\":\"2026-06-07T08:09:10+00:00\",\"CreatedByUserID\":\"111\",\"LastSavedByUserID\":\"222\",\"IsDeleted\":false},\"BranchID\":\"80\",\"ItemType\":\"Branch\",\"CustomFields\":{},\"RegionID\":50,\"CityID\":60,\"CompanyID\":70,\"CountryID\":40,\"CompanyBranchID\":88,\"DisplayOrder\":6,\"DisplayName\":\"Main Branch\",\"Description\":\"desc\",\"WebsiteURL\":null,\"PublishTargets\":[],\"id\":\"80\",\"CreateDate\":\"2026-01-02T03:04:05+00:00\",\"LastSaveDate\":\"2026-06-07T08:09:10+00:00\",\"CreatedByUserID\":\"111\",\"LastSavedByUserID\":\"222\",\"IsDeleted\":true}";

    [Fact] public void Brand() => AssertGolden(M.Map<BrandModel>(F.Brand()), BrandGolden);
    [Fact] public void Service() => AssertGolden(M.Map<ServiceModel>(F.Service()), ServiceGolden);
    [Fact] public void Department() => AssertGolden(M.Map<DepartmentModel>(F.Department()), DepartmentGolden);
    [Fact] public void Country() => AssertGolden(M.Map<CountryModel>(F.Country()), CountryGolden);
    [Fact] public void Region() => AssertGolden(M.Map<RegionModel>(F.Region()), RegionGolden);
    [Fact] public void Region_AsCityRegion() => AssertGolden(M.Map<CityRegionModel>(F.Region()), RegionCityRegionGolden);
    [Fact] public void City() => AssertGolden(M.Map<CityModel>(F.City()), CityGolden);
    [Fact] public void City_AsCompanyBranch() => AssertGolden(M.Map<CityCompanyBranchModel>(F.City()), CityCompanyBranchGolden);
    [Fact] public void Company() => AssertGolden(M.Map<CompanyModel>(F.Company()), CompanyGolden);
    [Fact] public void CompanyBranch() => AssertGolden(M.Map<BranchModel>(F.CompanyBranch()), CompanyBranchGolden, BranchLists);
    [Fact] public void User() => AssertGolden(M.Map<UserModel>(F.User()), UserGolden);
    [Fact] public void Team() => AssertGolden(M.Map<TeamModel>(F.Team()), TeamGolden);
    // ── The ACTUAL runtime case ───────────────────────────────────────────────────────────────────────────
    // The join row is inserted carrying only its FK, so the CompanyBranch navigation is null. The mapping must
    // null-propagate to a null name, not throw: a non-null-safe map NREs here and silently kills replication,
    // because the failure lands inside a swallowed per-row catch and the watermark still stamps clean. This golden
    // is the build-enforced record of that behaviour — the mapper reads every navigation through a ternary (an
    // expression tree cannot carry `?.`). Note BranchID pinned as "0": the mapper's deliberate
    // `s.CompanyBranch == null ? 0 : s.CompanyBranch.ID`, AutoMapper's default(long). A future reader who "fixes"
    // that to null changes live document content in a partitioned store; this is what stops them.

    [Fact] public void Team_NullBranchNav() => AssertGolden(M.Map<TeamModel>(F.Team(withBranchNav: false)), Team_NullBranchNavGolden);
    // ── Tombstones ────────────────────────────────────────────────────────────────────────────────────────
    // Every other fixture is IsDeleted = false, so the deleted-row document — the entire reason replication
    // propagates the flag at all — had zero coverage across all 22 original facts.

    [Fact] public void Brand_Deleted() => AssertGolden(M.Map<BrandModel>(F.Brand(deleted: true)), Brand_DeletedGolden);
    [Fact] public void CompanyBranch_Deleted() => AssertGolden(M.Map<BranchModel>(F.CompanyBranch(deleted: true)), CompanyBranch_DeletedGolden, BranchLists);
}
