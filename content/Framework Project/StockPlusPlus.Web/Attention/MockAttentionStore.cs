namespace StockPlusPlus.Web.Attention;

/// <summary>
/// UI-only prototype store. Assigns deterministic mock signals based on entity ID hash
/// and keeps cleared state + "seen" entities in memory for the session so stakeholders
/// can interact with acknowledge/clear flows and a cross-entity attention page.
/// </summary>
public class MockAttentionStore
{
    private readonly HashSet<string> cleared = new();
    private readonly Dictionary<string, SeenEntity> seen = new();

    // Fires when the active-signal set changes so lists can re-render without a full reload.
    public event Action? Changed;

    public IReadOnlyList<AttentionSignal> GetActive(string entityType, string? entityId)
    {
        if (string.IsNullOrEmpty(entityId)) return Array.Empty<AttentionSignal>();
        var key = Key(entityType, entityId);
        if (cleared.Contains(key)) return Array.Empty<AttentionSignal>();
        return BuildSignals(entityType, entityId);
    }

    public bool HasActive(string entityType, string? entityId) =>
        GetActive(entityType, entityId).Count > 0;

    public AttentionSeverity? HighestSeverity(string entityType, string? entityId)
    {
        var signals = GetActive(entityType, entityId);
        if (signals.Count == 0) return null;
        return signals.Max(s => s.Severity);
    }

    public void Clear(string entityType, string? entityId)
    {
        if (string.IsNullOrEmpty(entityId)) return;
        if (cleared.Add(Key(entityType, entityId)))
        {
            Changed?.Invoke();
        }
    }

    public void ResetCleared()
    {
        if (cleared.Count == 0) return;
        cleared.Clear();
        Changed?.Invoke();
    }

    public IReadOnlyList<AttentionHistoryEntry> GetHistory(string entityType, string? entityId)
    {
        if (string.IsNullOrEmpty(entityId)) return Array.Empty<AttentionHistoryEntry>();

        var key = Key(entityType, entityId);
        var current = BuildSignals(entityType, entityId);
        var isCleared = cleared.Contains(key);
        var hash = Math.Abs(StableHash(key));

        var history = new List<AttentionHistoryEntry>();

        // Currently-raised signals (active, or cleared recently if user acknowledged).
        foreach (var signal in current)
        {
            history.Add(new AttentionHistoryEntry(
                Signal: signal,
                IsActive: !isCleared,
                ClearedAt: isCleared ? DateTimeOffset.Now.AddMinutes(-(2 + hash % 6)) : null));
        }

        // Synthetic prior cleared signals so the history screen has something to show.
        if (hash % 2 == 0)
        {
            history.Add(new AttentionHistoryEntry(
                Signal: new AttentionSignal
                {
                    Category = "DateOverdue",
                    Source = "FrameworkOverdue",
                    Severity = AttentionSeverity.Warning,
                    Reason = "Previously flagged as overdue.",
                    RaisedAt = DateTimeOffset.Now.AddDays(-(3 + hash % 10)),
                },
                IsActive: false,
                ClearedAt: DateTimeOffset.Now.AddDays(-(2 + hash % 8))));
        }

        if (hash % 3 == 0)
        {
            history.Add(new AttentionHistoryEntry(
                Signal: new AttentionSignal
                {
                    Category = "ManualReview",
                    Source = "AppReview",
                    Severity = AttentionSeverity.Info,
                    Reason = "Earlier manual review flag.",
                    RaisedAt = DateTimeOffset.Now.AddDays(-(8 + hash % 5)),
                },
                IsActive: false,
                ClearedAt: DateTimeOffset.Now.AddDays(-(7 + hash % 4))));
        }

        if (hash % 5 == 0 && entityType == "Invoice")
        {
            history.Add(new AttentionHistoryEntry(
                Signal: new AttentionSignal
                {
                    Category = "BudgetExceeded",
                    Source = "AppBudget",
                    Severity = AttentionSeverity.Critical,
                    Reason = "Historically exceeded budget threshold.",
                    RaisedAt = DateTimeOffset.Now.AddDays(-(20 + hash % 10)),
                },
                IsActive: false,
                ClearedAt: DateTimeOffset.Now.AddDays(-(18 + hash % 9))));
        }

        return history
            .OrderByDescending(h => h.IsActive)
            .ThenByDescending(h => h.ClearedAt ?? h.Signal.RaisedAt)
            .ToList();
    }

    public void Track(string entityType, string entityId, string label, string detailRoute)
    {
        if (string.IsNullOrEmpty(entityId)) return;
        seen[Key(entityType, entityId)] = new SeenEntity(entityType, entityId, label, detailRoute);
    }

    public IReadOnlyList<EntityAttentionRef> AllActive()
    {
        var refs = new List<EntityAttentionRef>();
        foreach (var (key, entity) in seen)
        {
            if (cleared.Contains(key)) continue;
            var signals = BuildSignals(entity.EntityType, entity.EntityId);
            if (signals.Count == 0) continue;

            refs.Add(new EntityAttentionRef(
                EntityType: entity.EntityType,
                EntityId: entity.EntityId,
                EntityLabel: entity.Label,
                DetailRoute: entity.DetailRoute,
                Signals: signals));
        }
        return refs
            .OrderByDescending(r => r.Signals.Max(s => s.Severity))
            .ThenByDescending(r => r.Signals.Max(s => s.RaisedAt))
            .ToList();
    }

    private static IReadOnlyList<AttentionSignal> BuildSignals(string entityType, string entityId)
    {
        // Every entity always has signals for the prototype so demos don't fall into empty buckets.
        var bucket = Math.Abs(StableHash(entityType + "|" + entityId)) % 3;
        return (entityType, bucket) switch
        {
            ("Invoice", 0) => new[]
            {
                new AttentionSignal
                {
                    Category = "DateOverdue",
                    Source = "FrameworkOverdue",
                    Severity = AttentionSeverity.Warning,
                    Reason = "Invoice is past its due date.",
                    RaisedAt = DateTimeOffset.Now.AddDays(-2),
                },
            },
            ("Invoice", 1) => new[]
            {
                new AttentionSignal
                {
                    Category = "DateOverdue",
                    Source = "FrameworkOverdue",
                    Severity = AttentionSeverity.Warning,
                    Reason = "Invoice is 12 days past due date.",
                    RaisedAt = DateTimeOffset.Now.AddDays(-12),
                },
                new AttentionSignal
                {
                    Category = "BudgetExceeded",
                    Source = "AppBudget",
                    Severity = AttentionSeverity.Critical,
                    Reason = "Amount exceeds customer's approved budget.",
                    RaisedAt = DateTimeOffset.Now.AddHours(-6),
                },
            },
            ("Invoice", _) => new[]
            {
                new AttentionSignal
                {
                    Category = "ManualReview",
                    Source = "AppReview",
                    Severity = AttentionSeverity.Info,
                    Reason = "Flagged for manual review by accounting.",
                    RaisedAt = DateTimeOffset.Now.AddHours(-1),
                },
            },
            ("Product", 0) => new[]
            {
                new AttentionSignal
                {
                    Category = "LowStock",
                    Source = "AppStock",
                    Severity = AttentionSeverity.Warning,
                    Reason = "Stock below reorder threshold.",
                    RaisedAt = DateTimeOffset.Now.AddHours(-12),
                },
            },
            ("Product", 1) => new[]
            {
                new AttentionSignal
                {
                    Category = "PriceChanged",
                    Source = "AppPricing",
                    Severity = AttentionSeverity.Info,
                    Reason = "Price updated in the last 24 hours.",
                    RaisedAt = DateTimeOffset.Now.AddHours(-4),
                },
                new AttentionSignal
                {
                    Category = "LowStock",
                    Source = "AppStock",
                    Severity = AttentionSeverity.Critical,
                    Reason = "Out of stock — reorder urgently.",
                    RaisedAt = DateTimeOffset.Now.AddMinutes(-30),
                },
            },
            ("Product", _) => new[]
            {
                new AttentionSignal
                {
                    Category = "PriceChanged",
                    Source = "AppPricing",
                    Severity = AttentionSeverity.Info,
                    Reason = "Price updated in the last 24 hours.",
                    RaisedAt = DateTimeOffset.Now.AddHours(-4),
                },
            },
            _ => Array.Empty<AttentionSignal>(),
        };
    }

    private static int StableHash(string s)
    {
        unchecked
        {
            int hash = 23;
            foreach (var c in s) hash = hash * 31 + c;
            return hash;
        }
    }

    private static string Key(string entityType, string entityId) => $"{entityType}|{entityId}";

    private sealed record SeenEntity(string EntityType, string EntityId, string Label, string DetailRoute);
}
