using Microsoft.Extensions.Logging;
using ShiftSoftware.ShiftEntity.EFCore;
using StockPlusPlus.Data.Entities;
using System.Collections.Concurrent;

namespace StockPlusPlus.Data.SaveHandlers;

/// <summary>
/// After commit. Tells the price feed about the products whose price changed, once the change is committed: a save that
/// rolls back tells nobody. It is declared by the host (<c>AddSaveHandler</c> in Program.cs), because the feed is the
/// host's service.
/// </summary>
/// <remarks>
/// <c>[NeedsOriginal]</c> asks the save to keep each row's values as they were before it. Without it,
/// <c>row.Original</c> throws here.
/// </remarks>
[NeedsOriginal]
public sealed class ProductPriceFeed(IPriceFeed feed) : IAfterCommitHandler<Product>
{
    public async ValueTask AfterCommitAsync(SavedRows<Product> rows, CancellationToken cancellationToken)
    {
        var changes = new List<PriceChange>();

        foreach (var row in rows)
            if (row.Change == RowChange.Modified && row.Original!.Price != row.Entity.Price)
                changes.Add(new PriceChange(row.Entity.ID, row.Entity.Name, row.Original.Price, row.Entity.Price));

        // A failure here undoes nothing: the products are committed. The save call then throws an AfterCommitException,
        // and the API still answers that the product was saved.
        if (changes.Count > 0)
            await feed.PublishAsync(changes, cancellationToken);
    }
}

/// <summary>A product whose price changed.</summary>
public sealed record PriceChange(long ProductID, string Name, int? OldPrice, int? NewPrice);

/// <summary>Another system that follows the prices.</summary>
public interface IPriceFeed
{
    Task PublishAsync(IReadOnlyList<PriceChange> changes, CancellationToken cancellationToken);
}

/// <summary>
/// This sample's feed. It tells nobody: it keeps each change in memory and logs that it did. A real host implements the
/// same interface over a queue or another system's API.
/// </summary>
public sealed class SamplePriceFeed(ILogger<SamplePriceFeed> logger) : IPriceFeed
{
    private readonly ConcurrentQueue<PriceChange> published = new();

    /// <summary>The changes kept so far, oldest first.</summary>
    public IReadOnlyList<PriceChange> Published => published.ToArray();

    public Task PublishAsync(IReadOnlyList<PriceChange> changes, CancellationToken cancellationToken)
    {
        foreach (var change in changes)
            published.Enqueue(change);

        logger.LogInformation("Price changes kept, not sent: {Count}.", changes.Count);
        return Task.CompletedTask;
    }
}
