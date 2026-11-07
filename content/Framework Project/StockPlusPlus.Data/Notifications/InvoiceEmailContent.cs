using ShiftSoftware.ShiftEntity.Core.Notifications;
using StockPlusPlus.Data.Entities;

namespace StockPlusPlus.Data.Notifications;

/// <summary>The fields of an invoice's email. The framework's shared layout does the rest.</summary>
public class InvoiceEmailContent : INotificationEmailContent<Invoice>
{
    public ValueTask<NotificationEmailContent?> BuildAsync(NotificationTopic topic, Invoice record, CancellationToken cancellationToken = default)
        => ValueTask.FromResult<NotificationEmailContent?>(new NotificationEmailContent
        {
            Subject = $"New invoice | {record.InvoiceNo}",
            Headline = "A new invoice was saved",
            Details =
            [
                new("Invoice number", record.InvoiceNo.ToString()),
                new("Reference", record.ManualReference),
                new("Date", record.InvoiceDate?.ToString("d")),
            ],
        });
}
