using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MedStock.Data.Context;
using MedStock.Data.Entities;
using MedStock.Services.DTOs;
using MedStock.Services.Interfaces;

namespace MedStock.Services.Implementations
{
    public sealed class StocktakeService : IStocktakeService
    {
        private readonly DbExecutor _db;
        private readonly IInventoryService _inventory;
        private readonly Data.Context.IDbContextFactory<HospitalInventoryDbContext> _factory;

        public StocktakeService(DbExecutor db, IInventoryService inventory, Data.Context.IDbContextFactory<HospitalInventoryDbContext> factory)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public async Task<IReadOnlyList<StocktakeListRow>> GetListAsync(CancellationToken ct = default)
        {
            await using var db = _factory.CreateDbContext();
            return await db.Stocktakes.AsNoTracking()
                .OrderByDescending(x => x.StocktakeDate)
                .Select(x => new StocktakeListRow
                {
                    StocktakeId = x.StocktakeId,
                    StocktakeNo = x.StocktakeNo,
                    Date = x.StocktakeDate,
                    Status = x.Status,
                    CreatedBy = x.CreatedByUser.DisplayName,
                    Notes = x.Notes
                })
                .ToListAsync(ct);
        }

        public Task<int> CreateDraftAsync(int createdByUserId, string? notes, CancellationToken ct = default)
        {
            return _db.ExecuteAsync(async db =>
            {
                db.SetAuditUser(createdByUserId);

                // 1. إنشاء رأس الجرد (رقم فريد)
                var stocktake = new Stocktake
                {
                    StocktakeNo = await GenerateUniqueStocktakeNoAsync(db, ct),
                    StocktakeDate = DateTime.Now,
                    Status = "Draft",
                    Notes = notes,
                    CreatedByUserId = createdByUserId,
                    CreatedAt = DateTime.Now
                };
                db.Stocktakes.Add(stocktake);
                await db.SaveChangesAsync(ct); // للحصول على ID

                // 2. تجميد الأرصدة (Snapshot) لكل المواد الفعالة
                // نحسب الرصيد الحالي لكل مادة بجمع باتشاتها
                var itemBalances = await db.Batches
                    .Where(b => b.CurrentQty > 0) // المواد التي لها رصيد فقط
                    .GroupBy(b => b.ItemId)
                    .Select(g => new { ItemId = g.Key, TotalQty = g.Sum(b => b.CurrentQty) })
                    .ToListAsync(ct);

                // إضافة كل المواد للجدول (حتى التي رصيدها صفر لتأكيد العد)
                var allItems = await db.Items.Where(i => i.IsActive).Select(i => i.ItemId).ToListAsync(ct);

                var details = new List<StocktakeDetail>();
                foreach (var itemId in allItems)
                {
                    var balance = itemBalances.FirstOrDefault(x => x.ItemId == itemId)?.TotalQty ?? 0m;
                    details.Add(new StocktakeDetail
                    {
                        StocktakeId = stocktake.StocktakeId,
                        ItemId = itemId,
                        SystemQty = balance,
                        PhysicalQty = null, // لم يعد بعد
                        Difference = 0
                    });
                }

                db.StocktakeDetails.AddRange(details);
                await db.SaveChangesAsync(ct);

                return stocktake.StocktakeId;
            }, ct);
        }

        public async Task<IReadOnlyList<StocktakeItemRow>> GetDetailsAsync(int stocktakeId, CancellationToken ct = default)
        {
            await using var db = _factory.CreateDbContext();
            return await db.StocktakeDetails.AsNoTracking()
                .Where(x => x.StocktakeId == stocktakeId)
                .OrderBy(x => x.Item.ItemName)
                .Select(x => new StocktakeItemRow
                {
                    DetailId = x.StocktakeDetailId,
                    ItemId = x.ItemId,
                    ItemName = x.Item.ItemName,
                    Sku = x.Item.Sku,
                    Unit = x.Item.UnitOfMeasure,
                    SystemQty = x.SystemQty,
                    PhysicalQty = x.PhysicalQty
                })
                .ToListAsync(ct);
        }

        public Task SaveCountsAsync(int stocktakeId, Dictionary<long, decimal> counts, CancellationToken ct = default)
        {
            return _db.ExecuteAsync(async db =>
            {
                var st = await db.Stocktakes.FindAsync(new object[] { stocktakeId }, ct);
                if (st == null || st.Status != "Draft")
                    throw new InvalidOperationException("لا يمكن تعديل جرد غير موجود أو مرحل.");

                var details = await db.StocktakeDetails
                    .Where(d => d.StocktakeId == stocktakeId)
                    .ToListAsync(ct);

                foreach (var detail in details)
                {
                    if (counts.TryGetValue(detail.StocktakeDetailId, out var qty))
                    {
                        if (qty < 0) throw new InvalidOperationException("الكمية لا يمكن أن تكون سالبة.");
                        detail.PhysicalQty = qty;
                        detail.Difference = qty - detail.SystemQty;
                    }
                }
            }, ct);
        }

        public Task PostAsync(int stocktakeId, int postedByUserId, CancellationToken ct = default)
        {
            // ترحيل ذري واحد: كل التسويات + تحديث الحالة داخل نفس الـ Transaction.
            // الرصيد يتحرك عبر Trigger على TransactionDetails — ممنوع لمس Batches.CurrentQty يدوياً.
            return _db.ExecuteAsync(async db =>
            {
                db.SetAuditUser(postedByUserId);

                var st = await db.Stocktakes
                    .Include(x => x.StocktakeDetails)
                    .FirstOrDefaultAsync(x => x.StocktakeId == stocktakeId, ct);

                if (st == null || st.Status != "Draft")
                    throw new InvalidOperationException("الجرد غير صالح للترحيل.");

                foreach (var line in st.StocktakeDetails)
                {
                    if (line.PhysicalQty == null) continue;

                    decimal diff = line.PhysicalQty.Value - line.SystemQty;
                    if (diff == 0) continue;

                    if (diff > 0)
                    {
                        // فائض -> حركة إدخال تسوية
                        var reasonId = await ResolveReasonIdAsync(db, TransactionReasons.AdjIn, ct);

                        var trx = new Transaction
                        {
                            TransactionNo = await GenerateUniqueTransactionNoAsync(db, ct),
                            TransactionType = "I",
                            TransactionDate = DateTime.Now,
                            Notes = $"تسوية جرد {st.StocktakeNo} (زيادة)",
                            CreatedByUserId = postedByUserId,
                            ReasonId = reasonId
                        };
                        db.Transactions.Add(trx);
                        await db.SaveChangesAsync(ct);

                        var batch = new Batch
                        {
                            ItemId = line.ItemId,
                            BatchCode = $"ADJ-{DateTime.Today:yyMMdd}-{line.ItemId}-{Random.Shared.Next(100, 999)}",
                            ReceivedDate = DateOnly.FromDateTime(DateTime.Today),
                            ExpiryDate = DateOnly.FromDateTime(DateTime.Today.AddYears(1)),
                            InitialQty = diff,
                            CurrentQty = 0, // يزيده Trigger عند حفظ التفصيل
                            UnitCost = 0
                        };
                        db.Batches.Add(batch);
                        await db.SaveChangesAsync(ct);

                        db.TransactionDetails.Add(new TransactionDetail
                        {
                            TransactionId = trx.TransactionId,
                            BatchId = batch.BatchId,
                            Quantity = diff,
                            UnitCost = 0
                        });
                    }
                    else
                    {
                        // عجز -> حركة إخراج تسوية بتوزيع FEFO
                        var shortage = Math.Abs(diff);
                        var reasonId = await ResolveReasonIdAsync(db, TransactionReasons.AdjOut, ct);

                        var batches = await db.Batches.AsNoTracking()
                            .Where(b => b.ItemId == line.ItemId && b.CurrentQty > 0)
                            .OrderBy(b => b.ExpiryDate == null ? 1 : 0)
                            .ThenBy(b => b.ExpiryDate)
                            .ThenBy(b => b.ReceivedDate)
                            .ThenBy(b => b.BatchId)
                            .ToListAsync(ct);

                        decimal remaining = shortage;
                        var allocations = new List<(long batchId, decimal qty, decimal unitCost)>();
                        foreach (var batch in batches)
                        {
                            if (remaining <= 0m) break;
                            var take = Math.Min(remaining, batch.CurrentQty);
                            if (take <= 0m) continue;
                            allocations.Add((batch.BatchId, take, batch.UnitCost));
                            remaining -= take;
                        }

                        if (remaining > 0m)
                            throw new InvalidOperationException($"الرصيد غير كافٍ للمادة رقم {line.ItemId}. النقص المتبقي {remaining:0.###}.");

                        var trx = new Transaction
                        {
                            TransactionNo = await GenerateUniqueTransactionNoAsync(db, ct),
                            TransactionType = "O",
                            TransactionDate = DateTime.Now,
                            Notes = $"تسوية جرد {st.StocktakeNo} (عجز)",
                            CreatedByUserId = postedByUserId,
                            ReasonId = reasonId
                        };
                        db.Transactions.Add(trx);
                        await db.SaveChangesAsync(ct);

                        foreach (var (batchId, qty, unitCost) in allocations)
                        {
                            db.TransactionDetails.Add(new TransactionDetail
                            {
                                TransactionId = trx.TransactionId,
                                BatchId = batchId,
                                Quantity = qty,
                                UnitCost = unitCost
                            });
                        }
                    }
                }

                st.Status = "Posted";
                st.PostedAt = DateTime.Now;
                st.PostedByUserId = postedByUserId;
            }, ct);
        }

        public Task CancelAsync(int stocktakeId, int userId, CancellationToken ct = default)
        {
            return _db.ExecuteAsync(async db =>
            {
                var st = await db.Stocktakes.FindAsync(new object[] { stocktakeId }, ct);
                if (st != null && st.Status == "Draft")
                {
                    st.Status = "Cancelled";
                    st.CancelledAt = DateTime.Now;
                    st.CancelledByUserId = userId;
                }
            }, ct);
        }

        private static async Task<int?> ResolveReasonIdAsync(HospitalInventoryDbContext db, string reasonCode, CancellationToken ct)
        {
            var reason = await db.TransactionReasons.AsNoTracking()
                .Where(r => r.ReasonCode == reasonCode && r.IsActive)
                .Select(r => new { r.ReasonId })
                .FirstOrDefaultAsync(ct);

            if (reason is null)
                throw new InvalidOperationException($"سبب الحركة غير معرف أو غير فعال: '{reasonCode}'.");

            return reason.ReasonId;
        }

        private static async Task<string> GenerateUniqueTransactionNoAsync(HospitalInventoryDbContext db, CancellationToken ct)
        {
            for (int i = 0; i < 5; i++)
            {
                var no = $"TRX-{DateTime.Now:yyyyMMdd-HHmmss}-{Random.Shared.Next(1000, 9999)}";
                var exists = await db.Transactions.AsNoTracking().AnyAsync(t => t.TransactionNo == no, ct);
                if (!exists) return no;
            }
            throw new InvalidOperationException("فشل توليد رقم فريد للحركة.");
        }

        private static async Task<string> GenerateUniqueStocktakeNoAsync(HospitalInventoryDbContext db, CancellationToken ct)
        {
            for (int i = 0; i < 5; i++)
            {
                var no = $"STK-{DateTime.Now:yyMMdd}-{Random.Shared.Next(1000, 9999)}";
                var exists = await db.Stocktakes.AsNoTracking().AnyAsync(x => x.StocktakeNo == no, ct);
                if (!exists) return no;
            }
            throw new InvalidOperationException("فشل توليد رقم فريد للجرد.");
        }
    }
}