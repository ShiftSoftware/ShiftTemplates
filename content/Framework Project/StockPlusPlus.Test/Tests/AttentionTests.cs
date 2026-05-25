using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ShiftSoftware.ShiftEntity.Core.Attention;
using ShiftSoftware.ShiftEntity.EFCore.Entities;
using StockPlusPlus.Data.DbContext;
using StockPlusPlus.Data.Entities;
using StockPlusPlus.Data.Repositories;

namespace StockPlusPlus.Test.Tests;

[Collection("API Collection")]
public class AttentionTests
{
    private readonly CustomWebApplicationFactory factory;
    private readonly ITestOutputHelper output;

    public AttentionTests(CustomWebApplicationFactory factory, ITestOutputHelper output)
    {
        this.factory = factory;
        this.output = output;
    }

    [Fact]
    public void EvaluatorServices_AreRegistered()
    {
        using var scope = factory.Services.CreateScope();

        var overdue = scope.ServiceProvider.GetService<FrameworkOverdueEvaluator>();
        var missing = scope.ServiceProvider.GetService<StockPlusPlus.Data.Evaluators.InvoiceMissingReferenceEvaluator>();

        Assert.NotNull(overdue);
        Assert.NotNull(missing);
    }

    [Fact]
    public void OverdueEvaluator_DirectInvocation_ReturnsSignal()
    {
        var evaluator = new FrameworkOverdueEvaluator();
        var invoice = new Invoice { DueDate = DateTimeOffset.UtcNow.AddDays(-5) };
        var context = new AttentionContext<IHasDueDate>
        {
            Action = ShiftSoftware.ShiftEntity.Core.ActionTypes.Insert,
            Entity = invoice,
            Original = null,
            Services = factory.Services,
        };

        var signal = evaluator.Evaluate(context);

        Assert.NotNull(signal);
        Assert.Equal("Overdue", signal.Category);
        Assert.Equal(AttentionSeverity.Warning, signal.Severity);
    }

    [Fact]
    public void OverdueEvaluator_DirectInvocation_ReturnsNull_WhenNotOverdue()
    {
        var evaluator = new FrameworkOverdueEvaluator();
        var invoice = new Invoice { DueDate = DateTimeOffset.UtcNow.AddDays(30) };
        var context = new AttentionContext<IHasDueDate>
        {
            Action = ShiftSoftware.ShiftEntity.Core.ActionTypes.Insert,
            Entity = invoice,
            Original = null,
            Services = factory.Services,
        };

        var signal = evaluator.Evaluate(context);

        Assert.Null(signal);
    }

    [Fact]
    public void OverdueEvaluator_DirectInvocation_ReturnsNull_WhenNoDueDate()
    {
        var evaluator = new FrameworkOverdueEvaluator();
        var invoice = new Invoice { DueDate = null };
        var context = new AttentionContext<IHasDueDate>
        {
            Action = ShiftSoftware.ShiftEntity.Core.ActionTypes.Insert,
            Entity = invoice,
            Original = null,
            Services = factory.Services,
        };

        var signal = evaluator.Evaluate(context);

        Assert.Null(signal);
    }

    [Fact]
    public async Task Pipeline_RaisesSignal_WhenDueDateIsPast()
    {
        using var scope = factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<InvoiceRepository>();
        var db = scope.ServiceProvider.GetRequiredService<DB>();

        var invoice = new Invoice
        {
            InvoiceDate = DateTimeOffset.UtcNow.AddDays(-10),
            DueDate = DateTimeOffset.UtcNow.AddDays(-5),
            ManualReference = "ATT-PIPELINE-01",
            InvoiceNo = DateTimeOffset.UtcNow.Ticks,
        };

        repo.Add(invoice);
        await repo.SaveChangesAsync();

        Assert.True(invoice.HasActiveAttention);
        Assert.Equal(AttentionSeverity.Warning, invoice.HighestSeverity);
        Assert.True(invoice.ActiveSignalCount > 0);

        var signals = await db.Set<AttentionSignalEntry>()
            .Where(x => x.EntityType == "Invoice" && x.EntityId == invoice.ID)
            .ToListAsync();

        Assert.NotEmpty(signals);
        Assert.Contains(signals, s => s.Category == "Overdue" && s.Source == "FrameworkOverdueEvaluator");
    }

    [Fact]
    public async Task Pipeline_NoSignal_WhenDueDateIsInFuture()
    {
        using var scope = factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<InvoiceRepository>();
        var db = scope.ServiceProvider.GetRequiredService<DB>();

        var invoice = new Invoice
        {
            InvoiceDate = DateTimeOffset.UtcNow,
            DueDate = DateTimeOffset.UtcNow.AddDays(30),
            ManualReference = "ATT-NO-SIGNAL",
            InvoiceNo = DateTimeOffset.UtcNow.Ticks + 100,
        };

        repo.Add(invoice);
        await repo.SaveChangesAsync();

        Assert.False(invoice.HasActiveAttention);
        Assert.Null(invoice.HighestSeverity);
        Assert.Equal(0, invoice.ActiveSignalCount);

        var signals = await db.Set<AttentionSignalEntry>()
            .Where(x => x.EntityType == "Invoice" && x.EntityId == invoice.ID)
            .ToListAsync();

        Assert.Empty(signals);
    }

    [Fact]
    public async Task Pipeline_MissingReference_RaisesSignal()
    {
        using var scope = factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<InvoiceRepository>();
        var db = scope.ServiceProvider.GetRequiredService<DB>();

        var invoice = new Invoice
        {
            InvoiceDate = DateTimeOffset.UtcNow,
            ReleaseDate = DateTimeOffset.UtcNow.AddDays(-1),
            ManualReference = "",
            InvoiceNo = DateTimeOffset.UtcNow.Ticks + 200,
        };

        repo.Add(invoice);
        await repo.SaveChangesAsync();

        Assert.True(invoice.HasActiveAttention);

        var signals = await db.Set<AttentionSignalEntry>()
            .Where(x => x.EntityType == "Invoice" && x.EntityId == invoice.ID)
            .ToListAsync();

        Assert.Contains(signals, s => s.Category == "MissingReference" && s.Source == "InvoiceMissingReferenceEvaluator");
    }

    [Fact]
    public async Task MultiEvaluator_BothFireOnSameEntity()
    {
        using var scope = factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<InvoiceRepository>();
        var db = scope.ServiceProvider.GetRequiredService<DB>();

        var invoice = new Invoice
        {
            InvoiceDate = DateTimeOffset.UtcNow.AddDays(-10),
            DueDate = DateTimeOffset.UtcNow.AddDays(-5),
            ReleaseDate = DateTimeOffset.UtcNow.AddDays(-1),
            ManualReference = "",
            InvoiceNo = DateTimeOffset.UtcNow.Ticks + 300,
        };

        repo.Add(invoice);
        await repo.SaveChangesAsync();

        var signals = await db.Set<AttentionSignalEntry>()
            .Where(x => x.EntityType == "Invoice" && x.EntityId == invoice.ID)
            .ToListAsync();

        output.WriteLine($"Signal count: {signals.Count}");
        foreach (var s in signals)
            output.WriteLine($"  {s.Source}: {s.Category} ({s.Severity})");

        Assert.True(signals.Count >= 2, "Both evaluators should produce signals");
        Assert.Contains(signals, s => s.Source == "FrameworkOverdueEvaluator" && s.Category == "Overdue");
        Assert.Contains(signals, s => s.Source == "InvoiceMissingReferenceEvaluator" && s.Category == "MissingReference");

        Assert.Equal(AttentionSeverity.Warning, invoice.HighestSeverity);
        Assert.True(invoice.ActiveSignalCount >= 2);
    }

    [Fact]
    public async Task Dedup_SameSignalNotDuplicated_OnReSave()
    {
        using var scope = factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<InvoiceRepository>();
        var db = scope.ServiceProvider.GetRequiredService<DB>();

        var invoice = new Invoice
        {
            InvoiceDate = DateTimeOffset.UtcNow.AddDays(-10),
            DueDate = DateTimeOffset.UtcNow.AddDays(-5),
            ManualReference = "ATT-DEDUP",
            InvoiceNo = DateTimeOffset.UtcNow.Ticks + 400,
        };

        repo.Add(invoice);
        await repo.SaveChangesAsync();

        var countAfterInsert = await db.Set<AttentionSignalEntry>()
            .Where(x => x.EntityType == "Invoice" && x.EntityId == invoice.ID && x.Category == "Overdue")
            .CountAsync();

        Assert.Equal(1, countAfterInsert);

        invoice.ManualReference = "ATT-DEDUP-UPDATED";
        await repo.SaveChangesAsync();

        var countAfterUpdate = await db.Set<AttentionSignalEntry>()
            .Where(x => x.EntityType == "Invoice" && x.EntityId == invoice.ID && x.Category == "Overdue")
            .CountAsync();

        output.WriteLine($"Overdue signals after insert: {countAfterInsert}, after update: {countAfterUpdate}");
        Assert.Equal(1, countAfterUpdate);
    }

    [Fact]
    public async Task ClearFlow_ClearsSignals_ResetsSummary()
    {
        using var scope = factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<InvoiceRepository>();
        var db = scope.ServiceProvider.GetRequiredService<DB>();

        var invoice = new Invoice
        {
            InvoiceDate = DateTimeOffset.UtcNow.AddDays(-10),
            DueDate = DateTimeOffset.UtcNow.AddDays(-5),
            ManualReference = "ATT-CLEAR",
            InvoiceNo = DateTimeOffset.UtcNow.Ticks + 500,
        };

        repo.Add(invoice);
        await repo.SaveChangesAsync();

        Assert.True(invoice.HasActiveAttention);

        var now = DateTimeOffset.UtcNow;
        var signalEntries = await db.Set<AttentionSignalEntry>()
            .Where(x => x.EntityType == "Invoice" && x.EntityId == invoice.ID && x.ClearedAt == null)
            .ToListAsync();

        foreach (var s in signalEntries)
        {
            s.ClearedAt = now;
            s.ClearedByUserId = null;
        }

        invoice.HasActiveAttention = false;
        invoice.HighestSeverity = null;
        invoice.ActiveSignalCount = 0;
        await db.SaveChangesAsync();

        await db.Entry(invoice).ReloadAsync();

        output.WriteLine($"After clear: HasActiveAttention={invoice.HasActiveAttention}, " +
                         $"HighestSeverity={invoice.HighestSeverity}, ActiveSignalCount={invoice.ActiveSignalCount}");

        Assert.False(invoice.HasActiveAttention);
        Assert.Null(invoice.HighestSeverity);
        Assert.Equal(0, invoice.ActiveSignalCount);

        var signals = await db.Set<AttentionSignalEntry>()
            .Where(x => x.EntityType == "Invoice" && x.EntityId == invoice.ID)
            .ToListAsync();

        Assert.All(signals, s => Assert.NotNull(s.ClearedAt));
    }

    [Fact]
    public async Task ClearThenReSave_RaisesNewSignal()
    {
        using var scope = factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<InvoiceRepository>();
        var db = scope.ServiceProvider.GetRequiredService<DB>();

        var invoice = new Invoice
        {
            InvoiceDate = DateTimeOffset.UtcNow.AddDays(-10),
            DueDate = DateTimeOffset.UtcNow.AddDays(-5),
            ManualReference = "ATT-CLEAR-RERAISE",
            InvoiceNo = DateTimeOffset.UtcNow.Ticks + 600,
        };

        repo.Add(invoice);
        await repo.SaveChangesAsync();

        Assert.True(invoice.HasActiveAttention);

        var now = DateTimeOffset.UtcNow;
        var signalEntries = await db.Set<AttentionSignalEntry>()
            .Where(x => x.EntityType == "Invoice" && x.EntityId == invoice.ID && x.ClearedAt == null)
            .ToListAsync();

        foreach (var s in signalEntries)
            s.ClearedAt = now;

        invoice.HasActiveAttention = false;
        invoice.HighestSeverity = null;
        invoice.ActiveSignalCount = 0;
        await db.SaveChangesAsync();

        await db.Entry(invoice).ReloadAsync();
        Assert.False(invoice.HasActiveAttention);

        invoice.ManualReference = "ATT-CLEAR-RERAISE-V2";
        await repo.SaveChangesAsync();

        await db.Entry(invoice).ReloadAsync();

        output.WriteLine($"After re-save: HasActiveAttention={invoice.HasActiveAttention}, " +
                         $"ActiveSignalCount={invoice.ActiveSignalCount}");

        Assert.True(invoice.HasActiveAttention);

        var activeSignals = await db.Set<AttentionSignalEntry>()
            .Where(x => x.EntityType == "Invoice" && x.EntityId == invoice.ID && x.ClearedAt == null)
            .ToListAsync();

        Assert.NotEmpty(activeSignals);
        Assert.Contains(activeSignals, s => s.Category == "Overdue");
    }

    [Fact]
    public async Task NoSignal_WhenNoTriggerConditions()
    {
        using var scope = factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<InvoiceRepository>();
        var db = scope.ServiceProvider.GetRequiredService<DB>();

        var invoice = new Invoice
        {
            InvoiceDate = DateTimeOffset.UtcNow,
            DueDate = null,
            ReleaseDate = null,
            ManualReference = "ATT-CLEAN",
            InvoiceNo = DateTimeOffset.UtcNow.Ticks + 700,
        };

        repo.Add(invoice);
        await repo.SaveChangesAsync();

        Assert.False(invoice.HasActiveAttention);
        Assert.Null(invoice.HighestSeverity);
        Assert.Equal(0, invoice.ActiveSignalCount);

        var signals = await db.Set<AttentionSignalEntry>()
            .Where(x => x.EntityType == "Invoice" && x.EntityId == invoice.ID)
            .ToListAsync();

        Assert.Empty(signals);
    }

    [Fact]
    public async Task SummaryColumns_ReflectHighestSeverity()
    {
        using var scope = factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<InvoiceRepository>();
        var db = scope.ServiceProvider.GetRequiredService<DB>();

        var invoice = new Invoice
        {
            InvoiceDate = DateTimeOffset.UtcNow.AddDays(-10),
            DueDate = DateTimeOffset.UtcNow.AddDays(-5),
            ReleaseDate = DateTimeOffset.UtcNow.AddDays(-1),
            ManualReference = "",
            InvoiceNo = DateTimeOffset.UtcNow.Ticks + 800,
        };

        repo.Add(invoice);
        await repo.SaveChangesAsync();

        output.WriteLine($"HighestSeverity: {invoice.HighestSeverity} (Warning={AttentionSeverity.Warning}, Info={AttentionSeverity.Info})");

        Assert.True(invoice.HasActiveAttention);
        Assert.Equal(AttentionSeverity.Warning, invoice.HighestSeverity);
    }
}
