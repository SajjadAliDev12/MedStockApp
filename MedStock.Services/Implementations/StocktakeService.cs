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
                // 1. إنشاء رأس الجرد
                var stocktake = new Stocktake
                {
                    StocktakeNo = $"STK-{DateTime.Now:yyMMdd}-{Random.Shared.Next(1000, 9999)}",
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

        public async Task PostAsync(int stocktakeId, int postedByUserId, CancellationToken ct = default)
        {
            // عملية معقدة: يجب أن تكون خارج DbExecutor العادي لأنها ستستدعي InventoryService
            // التي بدورها تستخدم DbExecutor. لتجنب تداخل الـ Transactions، سننفذ المنطق بحذر.

            // 1. قراءة البيانات وحساب الفروقات
            await using var db = _factory.CreateDbContext();
            var st = await db.Stocktakes
                .Include(x => x.StocktakeDetails)
                .FirstOrDefaultAsync(x => x.StocktakeId == stocktakeId, ct);

            if (st == null || st.Status != "Draft") throw new InvalidOperationException("الجرد غير صالح للترحيل.");

            // 2. معالجة الفروقات
            foreach (var line in st.StocktakeDetails)
            {
                // إذا لم يدخل المستخدم قيمة، نعتبرها مطابقة للنظام (صفر فرق) أو يمكن اعتبارها 0 حسب السياسة
                // هنا سنفترض: Null = لم يتم الجرد (تجاهل)
                if (line.PhysicalQty == null) continue;

                decimal diff = line.PhysicalQty.Value - line.SystemQty;

                if (diff == 0) continue; // متطابق

                if (diff > 0)
                {
                    // زيادة (فائض) -> Stock In
                    await _inventory.StockInAsync(new StockInRequest
                    {
                        CreatedByUserId = postedByUserId,
                        ReasonCode = TransactionReasons.Purchase, // تأكد أن هذا الكود موجود في جدول TransactionReasons
                        Notes = $"تسوية جرد {st.StocktakeNo} (زيادة)",
                        Lines = new List<StockInLine>
                        {
                            new StockInLine
                            {
                                ItemId = line.ItemId,
                                Quantity = diff,
                                UnitCost = 0, // تكلفة صفرية للتسوية أو حسب سياسة المستشفى
                                BatchCode = $"ADJ-{DateTime.Today:yyMMdd}", // باتش تجميعي
                                ExpiryDate = DateOnly.FromDateTime(DateTime.Today.AddYears(1)) // افتراضي
                            }
                        }
                    }, ct);
                }
                else
                {
                    // نقص (عجز) -> Stock Out (القيمة سالبة، نحولها لموجبة للصرف)
                    await _inventory.StockOutAsync(new StockOutRequest
                    {
                        CreatedByUserId = postedByUserId,
                        ReasonCode = TransactionReasons.AdjOut,
                        Notes = $"تسوية جرد {st.StocktakeNo} (عجز)",
                        ItemId = line.ItemId,
                        Quantity = Math.Abs(diff)
                    }, ct);
                }
            }

            // 3. تحديث حالة الجرد
            // نستخدم DbExecutor هنا لتحديث الحالة فقط
            await _db.ExecuteAsync(async ctx =>
            {
                var s = await ctx.Stocktakes.FindAsync(new object[] { stocktakeId }, ct);
                if (s != null)
                {
                    s.Status = "Posted";
                    s.PostedAt = DateTime.Now;
                    s.PostedByUserId = postedByUserId;
                }
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
    }
}