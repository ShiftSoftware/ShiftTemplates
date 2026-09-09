using System.Net;
using System.Net.Http.Json;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using StockPlusPlus.Web.Development;
using Xunit;

namespace StockPlusPlus.Web.Tests;

public sealed class DevelopmentInboxTests
{
    [Fact]
    public void Inbox_shows_sender_failure_control_and_links_only_to_the_actual_same_origin_screens()
    {
        using var context = new BunitContext(); context.Services.AddMudServices(); context.JSInterop.Mode = JSRuntimeMode.Loose;
        var handler = new InboxHandler();
        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var cut = context.Render<DevelopmentInbox>(p => p.Add(x => x.Client, http));
        cut.WaitForAssertion(() => Assert.Contains("Local sender failures are enabled", cut.Markup));
        var link = Assert.Single(cut.FindAll("a"), a => a.TextContent == "Open link");
        Assert.Equal("http://localhost/Identity/ResetPassword#grant=opaque&purpose=PasswordResetEmail", link.GetAttribute("href"));
        Assert.Equal("_blank", link.GetAttribute("target"));
        Assert.Equal("noreferrer", link.GetAttribute("rel"));
        Assert.DoesNotContain("https://foreign.invalid", cut.Markup);
        Assert.Empty(handler.Posts);
        Assert.DoesNotContain("Try pending deliveries", cut.Markup);
        cut.FindAll("button").Single(b => b.TextContent == "Allow local delivery").Click();
        cut.WaitForAssertion(() => Assert.Contains("/development/inbox/failure/false", handler.Posts));
        cut.WaitForAssertion(() => Assert.Single(handler.Posts));
    }

    private sealed class InboxHandler : HttpMessageHandler
    {
        public List<string> Posts { get; } = [];
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Method == HttpMethod.Post)
            {
                Posts.Add(request.RequestUri!.AbsolutePath);
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            }
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new
            {
                messages = new[]
                {
                    new { id = "1", destination = "synthetic@example.invalid", subject = "Reset password", link = "/Identity/ResetPassword#grant=opaque&purpose=PasswordResetEmail", deliveredAt = DateTimeOffset.UtcNow },
                    new { id = "2", destination = "synthetic@example.invalid", subject = "Rejected link", link = "https://foreign.invalid/Identity/ResetPassword#grant=opaque", deliveredAt = DateTimeOffset.UtcNow }
                },
                failDeliveries = true
            }) });
        }
    }
}
