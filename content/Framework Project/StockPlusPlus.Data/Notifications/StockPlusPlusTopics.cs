using ShiftSoftware.ShiftEntity.Core.Notifications;
using StockPlusPlus.Shared.ActionTrees;
using System.IO;

namespace StockPlusPlus.Data.Notifications;

/// <summary>What this host can announce. The ID is stable, so a subscribers list can keep naming it.</summary>
public class StockPlusPlusTopics
{
    public static readonly NotificationTopic InvoiceCreated = new()
    {
        ID = "StockPlusPlus.Invoice.Created",
        DisplayName = "Invoice created",
        Description = "A new invoice was saved.",
        ReadPermission = StockPlusPlusActionTree.Invoice,
        AllowEmails = true,
    };

    /// <summary>Opens the subscribers list: who hears about each topic, with one section per environment.</summary>
    public static Stream? OpenSubscribersFile()
        => typeof(StockPlusPlusTopics).Assembly.GetManifestResourceStream("StockPlusPlus.Data.Notifications.notification-subscribers.json");
}
