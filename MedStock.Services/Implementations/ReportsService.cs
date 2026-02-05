using MedStock.Data.Context;
using MedStock.Services.DTOs;
using MedStock.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MedStock.Services.Implementations
{
    public class ReportsService : IReportsService
    {
        private readonly DbExecutor _db;

        public ReportsService(DbExecutor db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        
        public async Task<IReadOnlyList<StockCardRow>> GetStockCardAsync(int itemId, DateTime? from, DateTime? to, CancellationToken ct = default)
        {
            return await _db.ExecuteAsync<IReadOnlyList<StockCardRow>>(async db =>
            {
                
                decimal openingBalance = 0;

                if (from.HasValue)
                {
                    
                    openingBalance = await db.TransactionDetails
                        .AsNoTracking()
                        .Where(td => td.Batch.ItemId == itemId &&
                                     td.Transaction.TransactionDate < from.Value)
                        .SumAsync(td => td.Transaction.TransactionType == "I" ? td.Quantity : -td.Quantity, ct);
                }

                // 2. تجهيز الاستعلام
                var query = db.TransactionDetails
                    .AsNoTracking()
                    .Include(td => td.Transaction)
                    .ThenInclude(t => t.Department) // لجلب اسم القسم المستلم
                    .Include(td => td.Transaction)
                    .ThenInclude(t => t.Reason)     // لجلب سبب الحركة
                    .Where(td => td.Batch.ItemId == itemId);

                // فلتر التاريخ "من"
                if (from.HasValue)
                    query = query.Where(td => td.Transaction.TransactionDate >= from.Value);

                // فلتر التاريخ "إلى" (تصحيح المشكلة: نضيف يوم كامل لنشمل حركات اليوم الحالي)
                if (to.HasValue)
                {
                    var toDate = to.Value.Date.AddDays(1);
                    query = query.Where(td => td.Transaction.TransactionDate < toDate);
                }

                // 3. جلب البيانات من قاعدة البيانات
                var transactions = await query
                    .OrderBy(td => td.Transaction.TransactionDate)
                    .ThenBy(td => td.TransactionId)
                    .Select(td => new
                    {
                        td.Transaction.TransactionDate,
                        td.Transaction.TransactionType, // "I" or "O"
                        td.Transaction.TransactionNo,
                        td.Quantity, // الكمية من جدول التفاصيل كما طلبت
                        BaseNotes = td.Transaction.Notes,
                        DeptName = td.Transaction.Department != null ? td.Transaction.Department.DepartmentName : null,
                        ReasonName = td.Transaction.Reason != null ? td.Transaction.Reason.ReasonName : null
                    })
                    .ToListAsync(ct);

                // 4. معالجة النتائج وحساب الرصيد التراكمي
                var result = new List<StockCardRow>();
                decimal currentBalance = openingBalance;

                // إضافة سطر الرصيد الافتتاحي
                if (from.HasValue)
                {
                    result.Add(new StockCardRow
                    {
                        Date = from.Value.AddSeconds(-1),
                        TransactionType = "Initial",
                        Notes = "--- الرصيد الافتتاحي ---",
                        Balance = openingBalance
                    });
                }

                foreach (var t in transactions)
                {
                    // تحديد نوع الحركة للعرض
                    string typeDisplay;

                    if (t.TransactionType == "I")
                    {
                        currentBalance += t.Quantity;
                        typeDisplay = "وارد";
                    }
                    else
                    {
                        currentBalance -= t.Quantity;
                        typeDisplay = "صادر"; // صرف
                    }

                    // تنسيق الملاحظات لتوضيح أين ذهبت الكمية
                    var detailsParts = new List<string>();
                    if (!string.IsNullOrWhiteSpace(t.ReasonName)) detailsParts.Add(t.ReasonName);
                    if (!string.IsNullOrWhiteSpace(t.DeptName)) detailsParts.Add($"إلى: {t.DeptName}");
                    if (!string.IsNullOrWhiteSpace(t.BaseNotes)) detailsParts.Add(t.BaseNotes);

                    string finalNotes = string.Join(" - ", detailsParts);

                    result.Add(new StockCardRow
                    {
                        Date = t.TransactionDate,
                        TransactionType = typeDisplay,
                        InQty = t.TransactionType == "I" ? t.Quantity : 0,
                        OutQty = t.TransactionType == "O" ? t.Quantity : 0,
                        Balance = currentBalance,
                        Notes = finalNotes
                    });
                }

                return result;
            }, ct);
        }
        // أضف هذه الدوال داخل كلاس ReportsService

        public async Task<IReadOnlyList<ConsumptionSummaryRow>> GetConsumptionSummaryAsync(DateTime? from, DateTime? to, int? categoryId, int? itemId, CancellationToken ct = default)
        {
            return await _db.ExecuteAsync<IReadOnlyList<ConsumptionSummaryRow>>(async db =>
            {
                // 1. نبدأ من جدول التفاصيل لأنه المحور
                var query = db.TransactionDetails.AsNoTracking()
                    .Where(td => td.Transaction.TransactionType == "O"); // O = Out (صرف فقط)

                // 2. تطبيق فلاتر التاريخ
                if (from.HasValue)
                    query = query.Where(td => td.Transaction.TransactionDate >= from.Value);

                if (to.HasValue)
                {
                    var toDate = to.Value.Date.AddDays(1);
                    query = query.Where(td => td.Transaction.TransactionDate < toDate);
                }

                // 3. فلترة حسب التصنيف (إذا تم تحديده)
                // ملاحظة: نفترض العلاقة M:N عبر جدول ItemCategories
                if (categoryId.HasValue && categoryId.Value > 0)
                {
                    query = query.Where(td => td.Batch.Item.ItemCategories
                        .Any(ic => ic.CategoryId == categoryId.Value));
                }

                // 4. فلترة مادة محددة
                if (itemId.HasValue && itemId.Value > 0)
                {
                    query = query.Where(td => td.Batch.ItemId == itemId.Value);
                }

                // 5. التجميع (Grouping) - أهم خطوة
                // نجمع حسب المادة لحساب إجمالي الكمية
                var grouped = query
                    .GroupBy(td => new
                    {
                        td.Batch.ItemId,
                        td.Batch.Item.ItemName,
                        td.Batch.Item.Sku,
                    })
                    .Select(g => new ConsumptionSummaryRow
                    {
                        ItemId = g.Key.ItemId,
                        ItemName = g.Key.ItemName,
                        Sku = g.Key.Sku,
                        TotalQty = g.Sum(x => x.Quantity),
                        TransactionCount = g.Count()
                    });

                // 6. التنفيذ والترتيب (الأكثر استهلاكاً أولاً)
                return await grouped
                    .Where(x => x.TotalQty > 0) // تجاهل الصفريات
                    .OrderByDescending(x => x.TotalQty)
                    .ToListAsync(ct);

            }, ct);
        }

        public async Task<IReadOnlyList<ConsumptionDetailRow>> GetConsumptionDetailsAsync(int itemId, DateTime? from, DateTime? to, CancellationToken ct = default)
        {
            return await _db.ExecuteAsync<IReadOnlyList<ConsumptionDetailRow>>(async db =>
            {
                var query = db.TransactionDetails.AsNoTracking()
                    .Include(td => td.Transaction)
                    .ThenInclude(t => t.Department)
                    .Include(td => td.Transaction)
                    .ThenInclude(t => t.Reason)
                    .Include(td => td.Transaction)
                    .ThenInclude(t => t.CreatedByUser) // لجلب اسم الموظف
                    .Where(td => td.Transaction.TransactionType == "O" && td.Batch.ItemId == itemId);

                if (from.HasValue)
                    query = query.Where(td => td.Transaction.TransactionDate >= from.Value);

                if (to.HasValue)
                {
                    var toDate = to.Value.Date.AddDays(1);
                    query = query.Where(td => td.Transaction.TransactionDate < toDate);
                }

                return await query
                    .OrderByDescending(td => td.Transaction.TransactionDate)
                    .Select(td => new ConsumptionDetailRow
                    {
                        TransactionId = td.TransactionId,
                        Date = td.Transaction.TransactionDate,
                        TransactionNo = td.Transaction.TransactionNo,
                        DepartmentName = td.Transaction.Department != null ? td.Transaction.Department.DepartmentName : "---",
                        Reason = td.Transaction.Reason != null ? td.Transaction.Reason.ReasonName : "",
                        Qty = td.Quantity,
                        Notes = td.Transaction.Notes,
                        UserName = td.Transaction.CreatedByUser != null ? td.Transaction.CreatedByUser.DisplayName : ""
                    })
                    .ToListAsync(ct);

            }, ct);
        }
        // ... دالة ExpiryReport تبقى كما هي ...
        public Task<IReadOnlyList<ExpiryReportRow>> GetExpiryReportAsync(int daysThreshold, CancellationToken ct = default)
        {
            return _db.ExecuteAsync<IReadOnlyList<ExpiryReportRow>>(async db =>
            {
                var today = DateOnly.FromDateTime(DateTime.Today);
                var thresholdDate = today.AddDays(daysThreshold);

                var rawData = await db.Batches.AsNoTracking()
                    .Where(b => b.CurrentQty > 0 && b.ExpiryDate.HasValue && b.ExpiryDate <= thresholdDate)
                    .OrderBy(b => b.ExpiryDate)
                    .Select(b => new
                    {
                        ItemName = b.Item.ItemName,
                        BatchNo = b.BatchCode,
                        ExpiryDate = b.ExpiryDate,
                        Qty = b.CurrentQty
                    })
                    .ToListAsync(ct);

                return (IReadOnlyList<ExpiryReportRow>)rawData.Select(b => new ExpiryReportRow
                {
                    ItemName = b.ItemName,
                    BatchNo = b.BatchNo,
                    ExpiryDate = b.ExpiryDate?.ToDateTime(TimeOnly.MinValue),
                    Qty = b.Qty
                }).ToList();
            }, ct);
        }
    }
}