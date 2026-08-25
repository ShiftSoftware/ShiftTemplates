using ShiftSoftware.ShiftEntity.Model.Replication.IdentityModels;
using ShiftSoftware.ShiftIdentity.Core.DTOs.CompanyBranch;
using System.Linq.Expressions;

namespace StockPlusPlus.Data.Projections;

/// <summary>
/// Queryable projections for the replicated Cosmos documents this service reads directly (as opposed to
/// the SQL entities, whose projections are source-generated).
/// </summary>
public static class CompanyBranchProjections
{
    /// <summary>
    /// <see cref="CompanyBranchModel"/> (Cosmos) -> <see cref="CompanyBranchListDTO"/>, for the
    /// <c>api/CosmosCompanyBranch</c> OData endpoint.
    /// </summary>
    /// <remarks>
    /// Replaces <c>mapper.ProjectTo&lt;CompanyBranchListDTO&gt;(query)</c> and the profile behind it. The
    /// expression is what the Cosmos LINQ provider translates to SQL, so every member here has to stay a plain
    /// member access, cast, or conditional — no helper calls.
    /// <para>
    /// It reproduces the previous mapping exactly, INCLUDING the members that mapping left empty. Those are
    /// called out individually below rather than silently omitted: under a convention mapper an unfilled column
    /// looks identical to one that has no data, and telling those apart afterwards meant re-deriving the
    /// convention. Pinned by <c>CosmosProjectionParityTests</c>.
    /// </para>
    /// </remarks>
    public static readonly Expression<Func<CompanyBranchModel, CompanyBranchListDTO>> ToListDTO =
        m => new CompanyBranchListDTO
        {
            ID = m.ID,
            IsDeleted = m.IsDeleted,

            Name = m.Name,
            DisplayName = m.DisplayName,
            ShortCode = m.ShortCode,
            IntegrationId = m.IntegrationId,

            TerminationDate = m.TerminationDate,
            CompanyTerminationDate = m.Company.TerminationDate,

            // The three FK strings are NOT consistent with each other, and that is preserved on purpose.
            // CityId was mapped explicitly in the old profile and yields null when the FK is null; RegionId and
            // CompanyId came from AutoMapper's long? -> string convention, which yields "" instead. So an absent
            // city serializes as null while an absent region serializes as an empty string, on the same DTO.
            // Parity is the job here, so both behaviours are reproduced exactly rather than tidied up — see
            // CosmosProjectionParityTests, which fails if either one moves. Worth fixing, but as a deliberate
            // API change, not as a side effect of dropping AutoMapper.
            CityId = m.CityID == null ? null : m.CityID.ToString(),
            City = m.City.Name,
            RegionId = m.RegionID == null ? "" : m.RegionID.ToString(),
            CompanyId = m.CompanyID == null ? "" : m.CompanyID.ToString(),
            Company = m.Company.Name,

            PublishTargets = m.PublishTargets,

            DisplayOrder = m.DisplayOrder,
            CityDisplayOrder = m.City.DisplayOrder,
            CompanyDisplayOrder = m.Company.DisplayOrder,

            // Deliberately not projected — the document has no source for them, and the mapping this replaced
            // did not fill them either:
            //   Region                 the branch carries RegionID but no region NAME (it is City.Region.Name,
            //                          which the old convention did not reach: flattening would have needed the
            //                          member to be called CityRegionName)
            //   RegionDisplayOrder     same reason — City.Region.DisplayOrder
            //   CountryDisplayOrder    no Country anywhere on the branch document
            //   Departments/Services/  the sub-items live as SEPARATE documents in the CompanyBranch container
            //   Brands                 (ItemType-discriminated), so a single-document projection cannot reach
            //                          them; they keep their empty-collection initializers
        };
}
