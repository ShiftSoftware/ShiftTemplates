using System;
using System.Collections.Generic;
using System.Linq;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using ShiftSoftware.ShiftEntity.Model.Enums;
using ShiftSoftware.ShiftEntity.Model.Replication.IdentityModels;
using ShiftSoftware.ShiftIdentity.Core.DTOs.CompanyBranch;
using StockPlusPlus.Data.Projections;
using StockPlusPlus.Test.Tests.Parity;
using Xunit;

namespace StockPlusPlus.Test.Tests;

/// <summary>
/// Pins <see cref="CompanyBranchProjections.ToListDTO"/> to the behaviour of the AutoMapper profile it replaced.
/// </summary>
/// <remarks>
/// The Cosmos read endpoints are the one place a DTO is projected from a REPLICATED DOCUMENT rather than from a
/// SQL entity, so the source generator never covered them and the profile was the only written description of
/// the mapping.
/// <para>
/// <b>Provenance of the expectations below.</b> They are not hand-reasoned. They were captured from
/// <c>mapper.ProjectTo&lt;CompanyBranchListDTO&gt;</c> running the real profile over these exact fixtures, and
/// the two arms were compared member-by-member until they agreed. They are frozen here as literals for the same
/// reason the replication goldens are: once AutoMapper is gone the oracle is gone permanently, and a test that
/// re-derives its expectation from the code under test proves nothing.
/// </para>
/// <para>
/// They are deliberately written out in full — including every member the mapping leaves EMPTY. Under a
/// convention mapper an unfilled column is indistinguishable from one that has no data, and that is precisely
/// the half a hand-written replacement gets wrong silently.
/// </para>
/// </remarks>
public class CosmosProjectionParityTests
{
    private static List<CompanyBranchModel> Fixtures() => new()
    {
        // Fully populated — every navigation present.
        new CompanyBranchModel
        {
            id = "11", IsDeleted = false,
            Name = "Erbil Branch", DisplayName = "Erbil", ShortCode = "ERB", IntegrationId = "INT-1",
            TerminationDate = new DateTime(2030, 1, 2),
            RegionID = 5, CityID = 7, CompanyID = 9, CountryID = 3, CompanyBranchID = 11,
            DisplayOrder = 4,
            PublishTargets = new List<PublishTarget> { PublishTarget.Website },
            City = new CityCompanyBranchModel
            {
                id = "7", Name = "Erbil", DisplayOrder = 2,
                Region = new CityRegionModel { id = "5", Name = "Kurdistan", DisplayOrder = 1 },
            },
            Company = new CompanyModel
            {
                id = "9", Name = "Shift", DisplayOrder = 6,
                TerminationDate = new DateTime(2031, 3, 4),
            },
        },

        // Every nullable scalar null — the branch that decides whether "no value" comes back as null or as "".
        new CompanyBranchModel
        {
            id = "12", IsDeleted = true,
            Name = "Nulls", DisplayName = null, ShortCode = null, IntegrationId = null,
            TerminationDate = null,
            RegionID = null, CityID = null, CompanyID = null, CountryID = null, CompanyBranchID = null,
            DisplayOrder = null,
            PublishTargets = null,
            City = new CityCompanyBranchModel { id = "0", Name = null!, DisplayOrder = null },
            Company = new CompanyModel { id = "0", Name = null!, DisplayOrder = null, TerminationDate = null },
        },
    };

    private static List<CompanyBranchListDTO> Expected() => new()
    {
        new CompanyBranchListDTO
        {
            ID = "11", IsDeleted = false,
            Name = "Erbil Branch", DisplayName = "Erbil", ShortCode = "ERB", IntegrationId = "INT-1",
            TerminationDate = new DateTime(2030, 1, 2),
            CompanyTerminationDate = new DateTime(2031, 3, 4),
            CityId = "7", City = "Erbil",
            RegionId = "5", Region = null,
            CompanyId = "9", Company = "Shift",
            PublishTargets = new List<PublishTarget> { PublishTarget.Website },
            DisplayOrder = 4,
            CityDisplayOrder = 2,
            CompanyDisplayOrder = 6,
            // never filled by this mapping:
            CountryDisplayOrder = null,
            RegionDisplayOrder = null,
            Departments = new List<ShiftEntitySelectDTO>(),
            Services = new List<ShiftEntitySelectDTO>(),
            Brands = new List<ShiftEntitySelectDTO>(),
        },

        new CompanyBranchListDTO
        {
            ID = "12", IsDeleted = true,
            Name = "Nulls", DisplayName = null, ShortCode = null, IntegrationId = null,
            TerminationDate = null,
            CompanyTerminationDate = null,

            // The inconsistency that makes this test worth having: CityId was mapped EXPLICITLY in the old
            // profile and comes back null, while RegionId and CompanyId came from AutoMapper's
            // long? -> string convention and come back as "". Same DTO, same kind of member, two answers.
            CityId = null, City = null,
            RegionId = "", Region = null,
            CompanyId = "", Company = null,

            PublishTargets = null,
            DisplayOrder = null,
            CityDisplayOrder = null,
            CompanyDisplayOrder = null,
            CountryDisplayOrder = null,
            RegionDisplayOrder = null,
            Departments = new List<ShiftEntitySelectDTO>(),
            Services = new List<ShiftEntitySelectDTO>(),
            Brands = new List<ShiftEntitySelectDTO>(),
        },
    };

    [Fact]
    public void Projection_reproduces_the_mapping_it_replaced()
    {
        var actual = Fixtures().AsQueryable()
            .Select(CompanyBranchProjections.ToListDTO)
            .ToList();

        var expected = Expected();

        Assert.Equal(expected.Count, actual.Count);

        var differences = expected
            .Zip(actual, (e, a) => (e, a))
            .SelectMany((pair, i) => MemberPathDiff.Compare(pair.e, pair.a, $"[{i}]"))
            .ToList();

        Assert.True(
            differences.Count == 0,
            "The Cosmos projection no longer matches the mapping it replaced:" +
            Environment.NewLine +
            string.Join(Environment.NewLine, differences.Select(d => "  " + d)));
    }
}
