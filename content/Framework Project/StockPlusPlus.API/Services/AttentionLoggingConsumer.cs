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
        // The ANSI escapes render a black-on-yellow " ⚡ ATTENTION " badge in any VT-capable
        // terminal (Windows Terminal, VS Code), making the line easy to spot in a busy
        // console. Demo-only trick: if this logger ever feeds a non-console sink (file,
        // Seq, App Insights), the escapes arrive as raw garbage there — a real consumer
        // should log plain text and leave highlighting to the sink/terminal.
        logger.LogInformation(
            "\x1b[1;30;43m ⚡ ATTENTION raised on {EntityType}/{EntityId}: [{Severity}] {Source}/{Category} — {Reason}\u001b[0m",
            attentionRaised.EntityType,
            attentionRaised.EntityId,
            attentionRaised.Signal.Severity,
            attentionRaised.Signal.Source,
            attentionRaised.Signal.Category,
            attentionRaised.Signal.Reason);

        return Task.CompletedTask;
    }
}
