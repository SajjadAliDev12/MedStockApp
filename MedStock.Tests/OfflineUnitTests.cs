using System;
using System.Collections.Generic;
using System.Linq;
using MedStock.Data.Context;
using MedStock.Services.DTOs;
using MedStock.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MedStock.Tests;

// NOTE: Guard, PasswordHasher and TransactionNoGenerator are internal to
// MedStock.Services with no InternalsVisibleTo, so they are NOT directly
// testable here. These offline unit tests cover the public API surface only.

public sealed class OfflineUnitTests
{
    // ---------- SessionContext ----------

    [Fact]
    public void SessionContext_Initially_NotAuthenticated()
    {
        var ctx = new SessionContext();
        Assert.False(ctx.IsAuthenticated);
        Assert.Null(ctx.CurrentUser);
    }

    [Fact]
    public void SessionContext_SetUser_Authenticates()
    {
        var ctx = new SessionContext();
        ctx.SetUser(new SessionUser { UserId = 1, Username = "u", DisplayName = "U" });
        Assert.True(ctx.IsAuthenticated);
        Assert.Equal(1, ctx.CurrentUser!.UserId);
    }

    [Fact]
    public void SessionContext_SetUser_Null_Throws()
    {
        var ctx = new SessionContext();
        Assert.Throws<ArgumentNullException>(() => ctx.SetUser(null!));
    }

    [Fact]
    public void SessionContext_Clear_Resets_And_Raises_Event()
    {
        var ctx = new SessionContext();
        ctx.SetUser(new SessionUser { UserId = 1, Username = "u", DisplayName = "U" });
        int raised = 0;
        ctx.SessionChanged += () => raised++;
        ctx.Clear();
        Assert.False(ctx.IsAuthenticated);
        Assert.Null(ctx.CurrentUser);
        Assert.Equal(1, raised);
    }

    // ---------- SessionUser.IsInRole ----------

    [Fact]
    public void SessionUser_IsInRole_Is_CaseInsensitive()
    {
        var user = new SessionUser { UserId = 1, Username = "u", Roles = new[] { "Admin" } };
        Assert.True(user.IsInRole("admin"));
        Assert.True(user.IsInRole("ADMIN", "Other"));
        Assert.False(user.IsInRole("User"));
    }

    [Fact]
    public void SessionUser_IsInRole_NullOrEmpty_ReturnsFalse()
    {
        var user = new SessionUser { UserId = 1, Username = "u" };
        Assert.False(user.IsInRole());
        Assert.False(user.IsInRole((string[])null!));
        Assert.False(user.IsInRole("", "   "));
    }

    // ---------- EfErrorTranslator ----------

    [Fact]
    public void EfErrorTranslator_PlainException_ReturnsMessage()
    {
        var ex = new InvalidOperationException("boom");
        Assert.Equal("boom", EfErrorTranslator.ToUserMessage(ex));
    }

    [Fact]
    public void EfErrorTranslator_DbUpdateException_WithInner_ReturnsInnerMessage()
    {
        var ex = new DbUpdateException("outer", new Exception("inner-cause"));
        Assert.Equal("inner-cause", EfErrorTranslator.ToUserMessage(ex));
    }

    [Fact]
    public void EfErrorTranslator_DbUpdateException_NoInner_ReturnsOwnMessage()
    {
        var ex = new DbUpdateException("outer-only");
        Assert.Equal("outer-only", EfErrorTranslator.ToUserMessage(ex));
    }

    // ---------- TransactionReasons ----------

    [Fact]
    public void TransactionReasons_Constants_Are_Distinct_NonEmpty()
    {
        var values = new[]
        {
            TransactionReasons.Purchase, TransactionReasons.Donation,
            TransactionReasons.ReturnIn, TransactionReasons.AdjIn,
            TransactionReasons.Initial, TransactionReasons.Dispense,
            TransactionReasons.Expired, TransactionReasons.Damage,
            TransactionReasons.AdjOut, TransactionReasons.ReqFulfill,
            TransactionReasons.STOCKTAKEOUT, TransactionReasons.STOCKTAKEIN,
        };
        Assert.All(values, v => Assert.False(string.IsNullOrWhiteSpace(v)));
        Assert.Equal(values.Length, values.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void TransactionReasons_Known_Values()
    {
        Assert.Equal("PURCHASE", TransactionReasons.Purchase);
        Assert.Equal("REQ_FULFILL", TransactionReasons.ReqFulfill);
    }

    // ---------- DTO defaults ----------

    [Fact]
    public void PagedResult_Defaults()
    {
        var p = new PagedResult<string>();
        Assert.NotNull(p.Items);
        Assert.Empty(p.Items);
        Assert.Equal(0, p.TotalCount);
        Assert.Equal(0, p.TotalPages);
    }

    [Fact]
    public void ItemFilter_Defaults()
    {
        var f = new ItemFilter();
        Assert.Equal(1, f.PageNumber);
        Assert.Equal(50, f.PageSize);
        Assert.Null(f.IsActive);
    }

    [Fact]
    public void StockInRequest_DefaultReason_IsPurchase()
    {
        var r = new StockInRequest();
        Assert.Equal(TransactionReasons.Purchase, r.ReasonCode);
        Assert.NotNull(r.Lines);
        Assert.Empty(r.Lines);
    }

    // ---------- ExpiryReportRow computed Status ----------

    [Fact]
    public void ExpiryReportRow_Expired_Status()
    {
        var row = new ExpiryReportRow { ExpiryDate = DateTime.Today.AddDays(-1) };
        Assert.True(row.DaysRemaining < 0);
        Assert.Equal("منتهية الصلاحية", row.Status);
    }

    [Fact]
    public void ExpiryReportRow_Critical_When_Within_30_Days()
    {
        var row = new ExpiryReportRow { ExpiryDate = DateTime.Today.AddDays(10) };
        Assert.Equal("حرجة جداً", row.Status);
    }

    [Fact]
    public void ExpiryReportRow_NearExpiry_When_Within_90_Days()
    {
        var row = new ExpiryReportRow { ExpiryDate = DateTime.Today.AddDays(60) };
        Assert.Equal("قريبة الانتهاء", row.Status);
    }

    [Fact]
    public void ExpiryReportRow_Valid_When_Far_Future()
    {
        var row = new ExpiryReportRow { ExpiryDate = DateTime.Today.AddDays(200) };
        Assert.Equal("سارية", row.Status);
    }

    [Fact]
    public void ExpiryReportRow_NullExpiry_Is_Sentinel_Valid()
    {
        var row = new ExpiryReportRow { ExpiryDate = null };
        Assert.Equal(9999, row.DaysRemaining);
        Assert.Equal("سارية", row.Status);
    }

    // ---------- Service ctor guards (offline, no DB touched) ----------

    [Fact]
    public void AlertsService_NullFactory_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new AlertsService(null!));
    }

    [Fact]
    public void DbExecutor_NullFactory_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new DbExecutor(null!));
    }

    private sealed class ExplodingFactory : MedStock.Data.Context.IDbContextFactory<HospitalInventoryDbContext>
    {
        public HospitalInventoryDbContext CreateDbContext() => throw new NotImplementedException();
    }

    [Fact]
    public async Task DbExecutor_NullAction_Throws_Before_Touching_Db()
    {
        var exec = new DbExecutor(new ExplodingFactory());
        await Assert.ThrowsAsync<ArgumentNullException>(() => exec.ExecuteAsync(null!));
    }
}
