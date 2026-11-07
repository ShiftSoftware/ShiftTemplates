using Microsoft.Extensions.Logging;
using ShiftSoftware.ShiftEntity.Core.Notifications;
using System.Collections.Concurrent;

namespace StockPlusPlus.Data.Notifications;

/// <summary>
/// This sample's sender. It sends nothing: it keeps each email in memory and logs that it did. A real host implements
/// the same interface over whatever it sends email with.
/// </summary>
public class SampleNotificationOutbox(ILogger<SampleNotificationOutbox> logger) : INotificationEmailSender
{
    private readonly ConcurrentQueue<NotificationEmailMessage> sent = new();

    /// <summary>The emails kept so far, oldest first.</summary>
    public IReadOnlyList<NotificationEmailMessage> Sent => sent.ToArray();

    public Task SendAsync(NotificationEmailMessage message, CancellationToken cancellationToken = default)
    {
        sent.Enqueue(message);
        logger.LogInformation("Notification email kept, not sent: {Subject}, to {Count} address(es).", message.Subject, message.To.Count);
        return Task.CompletedTask;
    }
}
