namespace StockPlusPlus.Web.Attention;

public sealed record AttentionSignal
{
    public required string Category { get; init; }
    public string? Source { get; init; }
    public string? Reason { get; init; }
    public AttentionSeverity Severity { get; init; } = AttentionSeverity.Info;
    public DateTimeOffset RaisedAt { get; init; } = DateTimeOffset.Now;
}
