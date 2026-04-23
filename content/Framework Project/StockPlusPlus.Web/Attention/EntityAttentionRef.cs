namespace StockPlusPlus.Web.Attention;

public sealed record EntityAttentionRef(
    string EntityType,
    string EntityId,
    string EntityLabel,
    string DetailRoute,
    IReadOnlyList<AttentionSignal> Signals);

public sealed record AttentionHistoryEntry(
    AttentionSignal Signal,
    bool IsActive,
    DateTimeOffset? ClearedAt);
