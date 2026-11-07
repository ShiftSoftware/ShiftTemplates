using Microsoft.Extensions.DependencyInjection;
using ShiftSoftware.ShiftEntity.Core.Notifications;
using StockPlusPlus.Data.Entities;
using StockPlusPlus.Data.Notifications;
using StockPlusPlus.Data.Repositories;
using System.Net;

namespace StockPlusPlus.Test.Tests;

/// <summary>
/// The whole path in this sample: save a record, and see the right email in the sample's outbox, which sends nothing. The
/// subscribers list is the JSON file embedded in the data project.
/// </summary>
[Collection("API Collection")]
public class NotificationTests(CustomWebApplicationFactory factory)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private async Task SaveInvoiceAsync(string reference)
    {
        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<InvoiceRepository>();

        repository.Add(new Invoice
        {
            InvoiceDate = DateTimeOffset.UtcNow,
            ManualReference = reference,
            InvoiceNo = DateTimeOffset.UtcNow.Ticks,
            CompanyID = 4,
        });

        await repository.SaveChangesAsync();
    }

    private async Task<NotificationEmailMessage> WaitForEmailAsync(string reference)
    {
        var outbox = factory.Services.GetRequiredService<SampleNotificationOutbox>();
        var deadline = DateTimeOffset.UtcNow.AddSeconds(30);

        while (DateTimeOffset.UtcNow < deadline)
        {
            var email = outbox.Sent.FirstOrDefault(message => message.TextBody.Contains(reference));

            if (email is not null)
                return email;

            await Task.Delay(25, Ct);
        }

        throw new TimeoutException($"No email for '{reference}'.");
    }

    [Fact]
    public async Task SavingARecord_SendsTheRightEmail_ThroughTheFile()
    {
        var reference = "Reference <" + Guid.NewGuid().ToString("N") + ">";

        await SaveInvoiceAsync(reference);

        var email = await WaitForEmailAsync(reference);

        Assert.Equal(["sales-team@example.com"], email.To);
        Assert.Equal(["sales-manager@example.com"], email.Cc);
        Assert.Empty(email.Bcc);
        Assert.StartsWith("New invoice | ", email.Subject);

        // What a person typed is encoded in the HTML, and the footer the host set is there.
        Assert.DoesNotContain(reference, email.HtmlBody);
        Assert.Contains(WebUtility.HtmlEncode(reference), email.HtmlBody);
        Assert.Contains("StockPlusPlus sample", email.HtmlBody);
    }

    [Fact]
    public void TheCommittedFile_HasNoInvalidEntries_InAnySection()
    {
        var validator = factory.Services.GetRequiredService<NotificationSubscribersEntryValidator>();

        using var sectionsFile = StockPlusPlusTopics.OpenSubscribersFile()!;
        var sections = NotificationSubscribersJson.GetSections(sectionsFile);

        Assert.Equal(["Development", "Test", "Production"], sections);

        foreach (var section in sections)
        {
            using var file = StockPlusPlusTopics.OpenSubscribersFile()!;

            Assert.Empty(NotificationSubscribersJson.ReadSection(file, section, validator).Errors);
        }
    }
}
