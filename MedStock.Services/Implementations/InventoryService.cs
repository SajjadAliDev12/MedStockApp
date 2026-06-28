using MedStock.Data.Context;
using MedStock.Data.Entities;
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
    public sealed class InventoryService : IInventoryService
    {
        private readonly DbExecutor _db;

        public InventoryService(DbExecutor db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        // ... (StockInAsync and other methods remain unchanged) ...

        // تم اختصار StockInAsync هنا للتركيز على الحل، إذا كنت تحتاج الكود كاملاً أخبرني
        public Task<long> StockInAsync(StockInRequest request, CancellationToken ct = default)
        {
            Guard.NotNull(request, nameof(request));
            Guard.Positive(request.CreatedByUserId, nameof(request.CreatedByUserId));
            Guard.NotNullOrWhiteSpace(request.ReasonCode, nameof(request.ReasonCode), 40);
            Guard.NotEmpty(request.Lines, nameof(request.Lines));

            foreach (var line in request.Lines)
            {
                Guard.Positive(line.ItemId, nameof(line.ItemId));
                Guard.NotNullOrWhiteSpace(line.BatchCode, nameof(line.BatchCode), 60);
                Guard.PositiveDecimal(line.Quantity, nameof(line.Quantity));
                if (line.UnitCost < 0m) throw new ArgumentOutOfRangeException(nameof(line.UnitCost), "Must be >= 0.");
                if (line.SupplierRef != null && line.SupplierRef.Length > 120) throw new ArgumentException("SupplierRef too long.", nameof(line.SupplierRef));
                if (line.LocationCode != null && line.LocationCode.Length > 50) throw new ArgumentException("LocationCode too long.", nameof(line.LocationCode));
            }

            return _db.ExecuteAsync<long>(async db =>
            {
                db.SetAuditUser(request.CreatedByUserId);
                var reasonId = await ResolveReasonIdAsync(db, request.ReasonCode, ct);

                // Create transaction header
                var trx = new Transaction
                {
                    TransactionNo = await GenerateUniqueTransactionNoAsync(db, ct),
                    TransactionType = "I",
                    TransactionDate = DateTime.Now,
                    Notes = request.Notes,
                    SupplierId = request.SupplierId,
                    DepartmentId = request.DepartmentId,
                    CreatedByUserId = request.CreatedByUserId,
                    ReasonId = reasonId
                };

                db.Transactions.Add(trx);
                await db.SaveChangesAsync(ct); // ensure TransactionId is generated for FK usage

                // For each line, handle batch logic
                foreach (var line in request.Lines)
                {
                    // Ensure Item exists
                    var itemExists = await db.Items.AsNoTracking().AnyAsync(x => x.ItemId == line.ItemId, ct);
                    if (!itemExists) throw new InvalidOperationException($"ItemId {line.ItemId} does not exist.");

                    // Check if Batch already exists
                    var existingBatch = await db.Batches
                        .Where(b => b.ItemId == line.ItemId && b.BatchCode == line.BatchCode)
                        .FirstOrDefaultAsync(ct);

                    long targetBatchId;

                    if (existingBatch != null)
                    {
                        // Use existing batch
                        targetBatchId = existingBatch.BatchId;

                        // ملاحظة: نفترض هنا أن النظام يعتمد على Trigger لتحديث الكمية في جدول Batches
                        // عند إدراج TransactionDetail، تماماً كما هو الحال في دالة StockOutAsync.
                        // إذا لم يكن هناك Trigger، سيحتاج هذا الجزء لتحديث يدوي للكمية:
                        // existingBatch.CurrentQty += line.Quantity;
                    }
                    else
                    {
                        // Create new batch
                        var receivedDate = line.ReceivedDate ?? DateOnly.FromDateTime(DateTime.Now);
                        DateOnly? expiry = line.ExpiryDate;

                        var batch = new Batch
                        {
                            ItemId = line.ItemId,
                            BatchCode = line.BatchCode,
                            ReceivedDate = receivedDate,
                            ExpiryDate = expiry,
                            LocationCode = line.LocationCode,
                            InitialQty = line.Quantity, // نحتفظ بالكمية الأولية كمرجع

                            // التصحيح هنا: اجعل الكمية الحالية 0
                            // لأن التريجر سيقوم بإضافة الكمية بمجرد حفظ TransactionDetail
                            CurrentQty = 0,

                            UnitCost = line.UnitCost
                        };
                        db.Batches.Add(batch);
                        await db.SaveChangesAsync(ct);
                        targetBatchId = batch.BatchId;
                    }

                    // Create Transaction Detail
                    var detail = new TransactionDetail
                    {
                        TransactionId = trx.TransactionId,
                        BatchId = targetBatchId,
                        Quantity = line.Quantity,
                        UnitCost = line.UnitCost,
                        SupplierRef = line.SupplierRef
                    };
                    db.TransactionDetails.Add(detail);
                }

                return trx.TransactionId;
            }, ct);
        }


        // ========================================================================
        // الدالة المصححة: StockOutAsync
        // ========================================================================
        public Task<long> StockOutAsync(StockOutRequest request, CancellationToken ct = default)
        {
            Guard.NotNull(request, nameof(request));
            Guard.Positive(request.CreatedByUserId, nameof(request.CreatedByUserId));
            Guard.Positive(request.ItemId, nameof(request.ItemId));
            Guard.PositiveDecimal(request.Quantity, nameof(request.Quantity));
            Guard.NotNullOrWhiteSpace(request.ReasonCode, nameof(request.ReasonCode), 40);

            if (request.RequisitionDetailId.HasValue &&
                !string.Equals(request.ReasonCode, TransactionReasons.ReqFulfill, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("عند الصرف بناءً على طلب، يجب أن يكون سبب الحركة REQ_FULFILL.");
            }

            return _db.ExecuteAsync<long>(async db =>
            {
                db.SetAuditUser(request.CreatedByUserId);

                var itemExists = await db.Items.AsNoTracking().AnyAsync(x => x.ItemId == request.ItemId, ct);
                if (!itemExists) throw new InvalidOperationException($"المادة رقم {request.ItemId} غير موجودة.");

                var reasonId = await ResolveReasonIdAsync(db, request.ReasonCode, ct);

                // ✅ قراءة فقط (بدون Tracking) لأن الرصيد يتحدث عبر Trigger
                var batches = await db.Batches.AsNoTracking()
                    .Where(b => b.ItemId == request.ItemId && b.CurrentQty > 0)
                    .OrderBy(b => b.ExpiryDate == null ? 1 : 0)
                    .ThenBy(b => b.ExpiryDate)
                    .ThenBy(b => b.ReceivedDate)
                    .ThenBy(b => b.BatchId)
                    .ToListAsync(ct);

                if (batches.Count == 0)
                    throw new InvalidOperationException("لا يوجد رصيد متوفر لهذه المادة.");

                decimal remaining = request.Quantity;
                var allocations = new List<(long batchId, decimal qty, decimal unitCost)>();

                foreach (var batch in batches)
                {
                    if (remaining <= 0m) break;

                    var take = Math.Min(remaining, batch.CurrentQty);
                    if (take <= 0m) continue;

                    // ❌ لا تخصم هنا
                    allocations.Add((batch.BatchId, take, batch.UnitCost));
                    remaining -= take;
                }

                if (remaining > 0m)
                    throw new InvalidOperationException($"الرصيد غير كافٍ. الكمية المتوفرة أقل من المطلوبة بـ {(Int32)remaining}.");

                var trx = new Transaction
                {
                    TransactionNo = await GenerateUniqueTransactionNoAsync(db, ct),
                    TransactionType = "O",
                    TransactionDate = DateTime.Now,
                    Notes = request.Notes,
                    DepartmentId = request.DepartmentId,
                    CreatedByUserId = request.CreatedByUserId,
                    ReasonId = reasonId,
                };

                db.Transactions.Add(trx);
                await db.SaveChangesAsync(ct);

                var createdDetails = new List<TransactionDetail>();

                foreach (var (batchId, qty, unitCost) in allocations)
                {
                    var td = new TransactionDetail
                    {
                        TransactionId = trx.TransactionId,
                        BatchId = batchId,
                        Quantity = qty,
                        UnitCost = unitCost,
                    };

                    db.TransactionDetails.Add(td);
                    createdDetails.Add(td);
                }

                // ✅ احفظ الآن حتى تتولد TransactionDetailId
                await db.SaveChangesAsync(ct);

                // 2) بعد توليد IDs، أضف روابط الطلب باستخدام TransactionDetailId
                if (request.RequisitionDetailId.HasValue)
                {
                    var reqDetailId = request.RequisitionDetailId.Value;
                    var now = DateTime.UtcNow;

                    foreach (var td in createdDetails)
                    {
                        db.RequisitionFulfillmentLinks.Add(new RequisitionFulfillmentLink
                        {
                            RequisitionDetailId = reqDetailId,
                            TransactionDetailId = td.TransactionDetailId, // ✅ صريح
                            FulfilledQty = td.Quantity,
                            CreatedAt = now
                        });
                    }

                    // ✅ احفظ الروابط (وهنا سيشتغل Trigger تحديث FulfilledQty)
                    await db.SaveChangesAsync(ct);
                }

                // ✅ هنا Trigger سيخصم الرصيد من Batches بناءً على تفاصيل الحركة
                //await db.SaveChangesAsync(ct);

                return trx.TransactionId;
            }, ct);
        }


        // دوال مساعدة (تبقيها كما هي)
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
            // محاولة توليد رقم فريد 5 مرات
            for (int i = 0; i < 5; i++)
            {
                var no = TransactionNoGenerator.NewTransactionNo(); // تأكد أن هذا الكلاس موجود لديك، أو استخدم المنطق السابق
                var exists = await db.Transactions.AsNoTracking().AnyAsync(t => t.TransactionNo == no, ct);
                if (!exists) return no;
            }
            throw new InvalidOperationException("فشل توليد رقم فريد للحركة.");
        }


        Task<List<TransactionReasonDto>> IInventoryService.GetReasonsAsync(string scope)
        {
            throw new NotImplementedException();
        }
    }
}