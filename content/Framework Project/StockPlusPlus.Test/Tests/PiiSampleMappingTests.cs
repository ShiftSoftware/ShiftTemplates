using System.Text.Json;
using Microsoft.Extensions.Options;
using ShiftMapper;
using ShiftSoftware.ShiftEntity.Core.Pii;
using StockPlusPlus.Data.Entities;
using StockPlusPlus.Shared.DTOs.SampleContact;

namespace StockPlusPlus.Test.Tests;

public class PiiSampleMappingTests
{
    private static readonly IMapper Mapper = ShiftMapper.Mapper.Create(typeof(SampleContact).Assembly);

    [Fact]
    public void Generated_view_and_list_maps_are_protected_before_serialization()
    {
        var entity = new SampleContact
        {
            Label = "Synthetic 1",
            Name = "Ada Example",
            Phone = "+964 750 000 0088",
            Email = "ada@example.test",
            Address = "42 Example Street",
            Identifier = "123456789"
        };
        var protector = new PiiDtoProtector(new PiiFieldProtection(
            new DefaultPiiMasker(Options.Create(new PiiOptions()))));

        var view = Mapper.Map<SampleContactDTO>(entity);
        var list = Mapper.ProjectTo<SampleContact, SampleContactListDTO>(new[] { entity }.AsQueryable()).Single();
        var minimalView = Mapper.Map<SampleContactMinimalDTO>(entity);
        var minimalList = Mapper.ProjectTo<SampleContact, SampleContactMinimalListDTO>(new[] { entity }.AsQueryable()).Single();
        protector.Protect(view);
        protector.Protect(list);
        protector.Protect(minimalView);
        protector.Protect(minimalList);

        var serialized = JsonSerializer.Serialize(new { view, list, minimalView, minimalList });
        Assert.Equal("•••• 0088", view.Phone?.Display);
        Assert.Equal("•••• 0088", list.Phone?.Display);
        Assert.Equal("A E", view.Name?.Display);
        Assert.Equal("•••• 0088", minimalView.Phone?.Display);
        Assert.Equal("•••• 0088", minimalList.Phone?.Display);
        Assert.Null(view.Phone?.Value);
        Assert.DoesNotContain("+964 750 000 0088", serialized);
        Assert.DoesNotContain("ada@example.test", serialized);
        Assert.DoesNotContain("42 Example Street", serialized);
        Assert.DoesNotContain("123456789", serialized);
    }

    [Fact]
    public void Generated_write_map_uses_value_and_never_display()
    {
        var entity = new SampleContact { Label = "Before", Phone = "stored" };
        var dto = new SampleContactDTO
        {
            Label = "After",
            Phone = new() { Display = "malicious display", Value = "synthetic replacement", Write = "replace" }
        };

        Mapper.Map(dto, entity);

        Assert.Equal("After", entity.Label);
        Assert.Equal("synthetic replacement", entity.Phone);
    }
}
