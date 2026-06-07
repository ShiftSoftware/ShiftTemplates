using ShiftSoftware.ShiftEntity.Core.Attention;

namespace StockPlusPlus.API.Services;

/// <summary>
/// Sample <see cref="IAttentionConsumer"/> proving the Phase 2 emission wiring end-to-end:
/// logs one structured line per raised signal. A real consumer (email sender, SMS, audit
/// writer, webhook) replaces the log call — registration and shape stay the same. Runs on
/// the framework's background drain loop after the raising save has committed, so a slow or
/// failing consumer never affects the save itself.
/// </summary>
public sealed class AttentionLoggingConsumer : IAttentionConsumer
{
    private readonly ILogger<AttentionLoggingConsumer> logger;

    public AttentionLoggingConsumer(ILogger<AttentionLoggingConsumer> logger)
        => this.logger = logger;

    public Task HandleAsync(AttentionRaised attentionRaised, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Attention raised on {EntityType}/{EntityId}: [{Severity}] {Source}/{Category} — {Reason}",
            attentionRaised.EntityType,
            attentionRaised.EntityId,
            attentionRaised.Signal.Severity,
            attentionRaised.Signal.Source,
            attentionRaised.Signal.Category,
            attentionRaised.Signal.Reason);

        return Task.CompletedTask;
    }
}
