using System.Text.Json;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ShiftSoftware.ShiftBlazor.Components;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Core.Pii;
using ShiftSoftware.TypeAuth.Core;
using StockPlusPlus.Shared.ActionTrees;
using StockPlusPlus.Shared.DTOs.SampleContact;
using StockPlusPlus.Web.Pages.SampleContact;
using Xunit;

namespace StockPlusPlus.Web.Tests;

public class PiiContactValidationTests : ShiftBlazorTestContext
{
    [Fact]
    public void Sample_contact_form_shows_inherited_required_and_raw_value_errors()
    {
        Services.AddShiftEntityPii();
        Services.RemoveAll<ITypeAuthService>();
        var grants = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            [nameof(PiiActionTree)] = new Dictionary<string, object>
            {
                [nameof(PiiActionTree.Reveal)] = new[] { Access.Maximum }
            },
            [nameof(StockPlusPlusActionTree)] = new Dictionary<string, object>
            {
                [nameof(StockPlusPlusActionTree.SampleContacts)] = new[] { Access.Read, Access.Write }
            }
        });
        Services.AddSingleton<ITypeAuthService>(new TypeAuthContextBuilder().AddAccessTree(grants)
            .AddActionTree<PiiActionTree>().AddActionTree<StockPlusPlusActionTree>().Build());
        var cut = Render<SampleContactForm>();
        var form = cut.FindComponent<ShiftEntityForm<SampleContactDTO>>();
        var fields = cut.FindComponents<ShiftPiiField>();
        Assert.False(form.Instance.Validate());
        Assert.Contains("Phone is required.", cut.Markup);
        fields[1].Find("input").Input(new string('x', 41));
        Assert.False(form.Instance.Validate());
        Assert.Contains("Phone must be at most 40 characters.", cut.Markup);
        fields[1].Find("input").Input("synthetic-phone");
        fields[2].Find("input").Input("bad-email");
        Assert.False(form.Instance.Validate());
        Assert.Contains("Enter a valid email address.", cut.Markup);
        fields[2].Find("input").Input("a@example.test");
        Assert.True(form.Instance.Validate());
        Assert.DoesNotContain("Phone is required.", cut.Markup);
        Assert.DoesNotContain("Enter a valid email address.", cut.Markup);
    }
}
