using ShiftSoftware.ShiftEntity.Core.Attention;
using StockPlusPlus.Data.Entities;

namespace StockPlusPlus.Data.Evaluators;

public sealed class InvoiceMissingReferenceEvaluator : IAttentionEvaluator<Invoice>
{
    public AttentionSignal? Evaluate(AttentionContext<Invoice> context)
    {
        if (context.Entity.ReleaseDate is null)
            return null;

        if (!string.IsNullOrWhiteSpace(context.Entity.ManualReference))
            return null;

        return new AttentionSignal
        {
            Category = "MissingReference",
            Source = nameof(InvoiceMissingReferenceEvaluator),
            Reason = "Invoice has been released without a manual reference number.",
            Severity = AttentionSeverity.Info,
        };
    }
}
