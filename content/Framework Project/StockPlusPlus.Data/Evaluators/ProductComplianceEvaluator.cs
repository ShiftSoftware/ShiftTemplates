using ShiftSoftware.ShiftEntity.Core.Attention;
using StockPlusPlus.Data.Entities;

namespace StockPlusPlus.Data.Evaluators;

/// <summary>
/// App-registered evaluator that runs <em>alongside</em> <see cref="Product"/>'s own
/// <c>IHasAttentionEvaluator&lt;Product&gt;</c> (the composition model). It flags a released product
/// that is missing its country of origin for compliance sign-off — a different business rule than
/// the entity's own "released without a price" check.
/// </summary>
/// <remarks>
/// The signal is raised in the <see cref="Scope"/> ("Compliance") clear scope. Because that is a
/// <em>named</em> scope, the form's clear-on-open (which clears only the default scope) leaves it
/// active: it is cleared by an explicit "Mark compliance reviewed" action
/// (<c>ShiftEntityForm.ClearAttention("Compliance")</c>) or per-signal from the banner's row
/// dismiss. Product's own "ReleasedWithoutPrice" signal, by contrast, is in the default scope and
/// auto-clears on open. Together they demonstrate scoped + per-signal clearing on one entity.
/// </remarks>
public sealed class ProductComplianceEvaluator : IAttentionEvaluator<Product>
{
    /// <summary>Clear scope shared with the form's scoped-clear trigger. Keep in sync with ProductForm.</summary>
    public const string Scope = "Compliance";

    public AttentionSignal? Evaluate(AttentionContext<Product> context)
    {
        // Transition-agnostic current-state rule: a released product with no country of origin
        // needs a compliance review. Returns null (raises nothing) otherwise.
        if (context.Entity.ReleaseDate is null || context.Entity.CountryOfOriginID is not null)
            return null;

        return new AttentionSignal
        {
            Category = "NeedsComplianceReview",
            Source = nameof(ProductComplianceEvaluator),
            Reason = "Released product is missing its country of origin.",
            Severity = AttentionSeverity.Warning,
            ClearScope = Scope,
        };
    }
}
