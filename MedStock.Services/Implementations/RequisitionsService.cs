using Azure.Core;
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
    public sealed class RequisitionsService : IRequisitionsService
    {
        private readonly DbExecutor _db;
        private readonly IInventoryService _inventory;

        public RequisitionsService(DbExecutor db, IInventoryService inventory)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
        }

        public Task<IReadOnlyList<DepartmentRow>> GetDepartmentsAsync(CancellationToken ct = default)
        {
            return _db.ExecuteAsync<IReadOnlyList<DepartmentRow>>(async db =>
            {
                return await db.Departments.AsNoTracking()
                    .OrderBy(d => d.DepartmentName)
                    .Select(d => new DepartmentRow
                    {
                        DepartmentId = d.DepartmentId,
                        DepartmentName = d.DepartmentName
                    })
                    .ToListAsync(ct);
            }, ct);
        }
        public Task<IReadOnlyList<PendingRequisitionRow>> GetPendingToIssueAsync(CancellationToken ct = default)
        {
            return _db.ExecuteAsync<IReadOnlyList<PendingRequisitionRow>>(async db =>
            {
                // نجلب السطور التي حالتها Approved ولم تكتمل بعد
                return await db.RequisitionDetails.AsNoTracking()
                    .Include(d => d.Requisition)
                    .ThenInclude(r => r.Department)
                    .Include(d => d.Item)
                    .Where(d => d.Requisition.Status == "Approved" && d.FulfilledQty < d.RequestedQty)
                    .OrderBy(d => d.Requisition.DecisionDate) // الأقدم أولاً
                    .Select(d => new PendingRequisitionRow
                    {
                        RequisitionDetailId = d.RequisitionDetailId,
                        RequisitionNo = d.Requisition.RequisitionNo,
                        DepartmentName = d.Requisition.Department.DepartmentName,
                        ItemId = d.ItemId,
                        ItemName = d.Item.ItemName,
                        RequestedQty = d.RequestedQty,
                        FulfilledQty = d.FulfilledQty,
                        RemainingQty = d.RequestedQty - d.FulfilledQty,
                        Notes = d.Requisition.Notes
                    })
                    .ToListAsync(ct);
            }, ct);
        }
        public Task<IReadOnlyList<RequisitionListRow>> GetListAsync(string? status, int? departmentId, string? search, CancellationToken ct = default)
        {
            return _db.ExecuteAsync<IReadOnlyList<RequisitionListRow>>(async db =>
            {
                var q = db.Requisitions.AsNoTracking()
                    .Include(r => r.Department)
                    .Include(r => r.RequestedByUser)
                    .Include(r => r.ApprovedByUser)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(status))
                    q = q.Where(r => r.Status == status);

                if (departmentId.HasValue && departmentId.Value > 0)
                    q = q.Where(r => r.DepartmentId == departmentId.Value);

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var s = search.Trim();
                    // تم التعديل: البحث الآن يشمل رقم الطلب، الملاحظات، اسم القسم، واسم الموظف
                    q = q.Where(r =>
                        r.RequisitionNo.Contains(s) ||
                        (r.Notes != null && r.Notes.Contains(s)) ||
                        r.Department.DepartmentName.Contains(s) ||
                        r.RequestedByUser.DisplayName.Contains(s)
                    );
                }

                return await q
                    .OrderByDescending(r => r.RequestDate)
                    .Select(r => new RequisitionListRow
                    {
                        RequisitionId = r.RequisitionId,
                        RequisitionNo = r.RequisitionNo,
                        DepartmentName = r.Department.DepartmentName,
                        Status = r.Status,
                        RequestDate = r.RequestDate,
                        RequestedBy = r.RequestedByUser.DisplayName,
                        ApprovedBy = r.ApprovedByUser != null ? r.ApprovedByUser.DisplayName : null
                    })
                    .ToListAsync(ct);
            }, ct);
        }

        public Task<(RequisitionHeaderDto header, IReadOnlyList<RequisitionDetailRow> lines)> GetDetailsAsync(long requisitionId, CancellationToken ct = default)
        {
            if (requisitionId <= 0) throw new InvalidOperationException("Invalid requisitionId.");

            return _db.ExecuteAsync<(RequisitionHeaderDto, IReadOnlyList<RequisitionDetailRow>)>(async db =>
            {
                var header = await db.Requisitions.AsNoTracking()
                    .Where(r => r.RequisitionId == requisitionId)
                    .Select(r => new RequisitionHeaderDto
                    {
                        RequisitionId = r.RequisitionId,
                        RequisitionNo = r.RequisitionNo,
                        DepartmentId = r.DepartmentId,
                        Status = r.Status,
                        RequestDate = r.RequestDate,
                        DecisionDate = r.DecisionDate,
                        Notes = r.Notes,
                        RequestedByUserId = r.RequestedByUserId,
                        ApprovedByUserId = r.ApprovedByUserId
                    })
                    .FirstOrDefaultAsync(ct);

                if (header is null) throw new InvalidOperationException("Requisition not found.");

                var lines = await db.RequisitionDetails.AsNoTracking()
                    .Where(l => l.RequisitionId == requisitionId)
                    .Join(db.Items.AsNoTracking(),
                          l => l.ItemId,
                          i => i.ItemId,
                          (l, i) => new RequisitionDetailRow
                          {
                              RequisitionDetailId = l.RequisitionDetailId,
                              ItemId = l.ItemId,
                              ItemName = i.ItemName,
                              Sku = i.Sku,
                              RequestedQty = l.RequestedQty,
                              FulfilledQty = l.FulfilledQty,
                              Notes = l.Notes
                          })
                    .OrderBy(x => x.ItemName)
                    .ToListAsync(ct);

                return (header, lines);
            }, ct);
        }

        public Task<long> CreateDraftAsync(int departmentId, string? notes, int requestedByUserId, CancellationToken ct = default)
        {
            Guard.Positive(departmentId, nameof(departmentId));
            Guard.Positive(requestedByUserId, nameof(requestedByUserId));

            return _db.ExecuteAsync<long>(async db =>
            {
                
                var deptExists = await db.Departments.AsNoTracking().AnyAsync(d => d.DepartmentId == departmentId, ct);
                if (!deptExists) throw new InvalidOperationException("Department not found.");

                var req = new Requisition
                {
                    RequisitionNo = await GenerateUniqueRequisitionNoAsync(db, ct),
                    DepartmentId = departmentId,
                    Status = "Draft",
                    RequestedByUserId = requestedByUserId,
                    RequestDate = DateTime.Now, 
                    Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim()
                };

                db.Requisitions.Add(req);
                await db.SaveChangesAsync(ct);

                return req.RequisitionId;
            }, ct);
        }

        public Task UpdateHeaderAsync(long requisitionId, int departmentId, string? notes, int userId, CancellationToken ct = default)
        {
            Guard.Positive(requisitionId, nameof(requisitionId));
            Guard.Positive(departmentId, nameof(departmentId));
            Guard.Positive(userId, nameof(userId));

            return _db.ExecuteAsync(async db =>
            {
                var req = await db.Requisitions.FirstOrDefaultAsync(r => r.RequisitionId == requisitionId, ct);
                if (req is null) throw new InvalidOperationException("Requisition not found.");

                EnsureEditable(req.Status);

                req.DepartmentId = departmentId;
                req.Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();

                await db.SaveChangesAsync(ct);
            }, ct);
        }

        public Task AddOrUpdateLineAsync(long requisitionId, int itemId, decimal requestedQty, string? notes, int userId, CancellationToken ct = default)
        {
            Guard.Positive(requisitionId, nameof(requisitionId));
            Guard.Positive(itemId, nameof(itemId));
            Guard.PositiveDecimal(requestedQty, nameof(requestedQty));
            Guard.Positive(userId, nameof(userId));

            return _db.ExecuteAsync(async db =>
            {
                var req = await db.Requisitions.FirstOrDefaultAsync(r => r.RequisitionId == requisitionId, ct);
                if (req is null) throw new InvalidOperationException("Requisition not found.");

                EnsureEditable(req.Status);

                var itemExists = await db.Items.AsNoTracking().AnyAsync(i => i.ItemId == itemId, ct);
                if (!itemExists) throw new InvalidOperationException("Item not found.");

                var line = await db.RequisitionDetails
                    .FirstOrDefaultAsync(l => l.RequisitionId == requisitionId && l.ItemId == itemId, ct);

                if (line is null)
                {
                    db.RequisitionDetails.Add(new RequisitionDetail
                    {
                        RequisitionId = requisitionId,
                        ItemId = itemId,
                        RequestedQty = requestedQty,
                        FulfilledQty = 0m,
                        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim()
                    });
                }
                else
                {
                    if (line.FulfilledQty > requestedQty)
                        throw new InvalidOperationException("لا يمكن جعل الكمية المطلوبة أقل من الكمية المصروفة بالفعل.");

                    line.RequestedQty = requestedQty;
                    line.Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
                }

                await db.SaveChangesAsync(ct);
            }, ct);
        }

        public Task RemoveLineAsync(long requisitionDetailId, int userId, CancellationToken ct = default)
        {
            Guard.Positive(requisitionDetailId, nameof(requisitionDetailId));
            Guard.Positive(userId, nameof(userId));

            return _db.ExecuteAsync(async db =>
            {
                var line = await db.RequisitionDetails
                    .Include(l => l.Requisition)
                    .FirstOrDefaultAsync(l => l.RequisitionDetailId == requisitionDetailId, ct);

                if (line is null) return;

                EnsureEditable(line.Requisition.Status);

                if (line.FulfilledQty > 0m)
                    throw new InvalidOperationException("لا يمكن حذف سطر تم صرف جزء منه.");

                db.RequisitionDetails.Remove(line);
                await db.SaveChangesAsync(ct);
            }, ct);
        }

        public Task SubmitAsync(long requisitionId, int userId, CancellationToken ct = default)
        {
            Guard.Positive(requisitionId, nameof(requisitionId));
            Guard.Positive(userId, nameof(userId));

            return _db.ExecuteAsync(async db =>
            {
                var req = await db.Requisitions.FirstOrDefaultAsync(r => r.RequisitionId == requisitionId, ct);
                if (req is null) throw new InvalidOperationException("Requisition not found.");

                EnsureEditable(req.Status);

                var hasLines = await db.RequisitionDetails.AsNoTracking().AnyAsync(l => l.RequisitionId == requisitionId, ct);
                if (!hasLines) throw new InvalidOperationException("لا يمكن إرسال طلب بدون مواد.");

                req.Status = "Submitted";
                await db.SaveChangesAsync(ct);
            }, ct);
        }

        public Task ApproveAsync(long requisitionId, int approvedByUserId, CancellationToken ct = default)
        {
            Guard.Positive(requisitionId, nameof(requisitionId));
            Guard.Positive(approvedByUserId, nameof(approvedByUserId));

            return _db.ExecuteAsync(async db =>
            {
                var req = await db.Requisitions.FirstOrDefaultAsync(r => r.RequisitionId == requisitionId, ct);
                if (req is null) throw new InvalidOperationException("Requisition not found.");

                if (req.Status != "Submitted")
                    throw new InvalidOperationException("يمكن اعتماد الطلب فقط إذا كان بحالة Submitted.");

                req.Status = "Approved";
                req.ApprovedByUserId = approvedByUserId;
                req.DecisionDate = DateTime.Now;

                await db.SaveChangesAsync(ct);
            }, ct);
        }

        public Task RejectAsync(long requisitionId, int rejectedByUserId, CancellationToken ct = default)
        {
            Guard.Positive(requisitionId, nameof(requisitionId));
            Guard.Positive(rejectedByUserId, nameof(rejectedByUserId));

            return _db.ExecuteAsync(async db =>
            {
                var req = await db.Requisitions.FirstOrDefaultAsync(r => r.RequisitionId == requisitionId, ct);
                if (req is null) throw new InvalidOperationException("Requisition not found.");

                if (req.Status != "Submitted")
                    throw new InvalidOperationException("يمكن رفض الطلب فقط إذا كان بحالة Submitted.");

                req.Status = "Rejected";
                req.ApprovedByUserId = rejectedByUserId;
                req.DecisionDate = DateTime.Now; 

                await db.SaveChangesAsync(ct);
            }, ct);
        }

        public Task CancelAsync(long requisitionId, int userId, CancellationToken ct = default)
        {
            Guard.Positive(requisitionId, nameof(requisitionId));
            Guard.Positive(userId, nameof(userId));

            return _db.ExecuteAsync(async db =>
            {
                var req = await db.Requisitions.FirstOrDefaultAsync(r => r.RequisitionId == requisitionId, ct);
                if (req is null) throw new InvalidOperationException("Requisition not found.");

                if (req.Status is not ("Draft" or "Submitted"))
                    throw new InvalidOperationException("يمكن إلغاء الطلب فقط إذا كان Draft أو Submitted.");

                req.Status = "Cancelled";
                await db.SaveChangesAsync(ct);
            }, ct);
        }

        public async Task<long> FulfillLineAsync(
            long requisitionDetailId,
            decimal qtyToFulfill,
            int createdByUserId,
            int? departmentId,
            string? notes,
            CancellationToken ct = default)
        {
            Guard.Positive(requisitionDetailId, nameof(requisitionDetailId));
            Guard.Positive(createdByUserId, nameof(createdByUserId));
            Guard.PositiveDecimal(qtyToFulfill, nameof(qtyToFulfill));

            var info = await _db.ExecuteAsync(async db =>
            {
                var row = await db.RequisitionDetails.AsNoTracking()
                    .Include(l => l.Requisition)
                    .Where(l => l.RequisitionDetailId == requisitionDetailId)
                    .Select(l => new
                    {
                        l.RequisitionDetailId,
                        l.ItemId,
                        l.RequisitionId,
                        Remaining = l.RequestedQty - l.FulfilledQty,
                        ReqStatus = l.Requisition.Status,
                        DeptId = l.Requisition.DepartmentId
                    })
                    .FirstOrDefaultAsync(ct);

                if (row is null) throw new InvalidOperationException("Requisition line not found.");
                if (row.ReqStatus != "Approved")
                    throw new InvalidOperationException("لا يمكن الصرف إلا لطلب بحالة Approved.");
                if (row.Remaining <= 0m)
                    throw new InvalidOperationException("هذا السطر مكتمل بالفعل.");

                return row;
            }, ct);

            var qty = qtyToFulfill > info.Remaining ? info.Remaining : qtyToFulfill;

            var trxId = await _inventory.StockOutAsync(new StockOutRequest
            {
                ItemId = info.ItemId,
                Quantity = qty,
                CreatedByUserId = createdByUserId,
                DepartmentId = departmentId ?? info.DeptId,
                Notes = notes,
                ReasonCode = TransactionReasons.ReqFulfill,
                RequisitionDetailId = info.RequisitionDetailId
            }, ct);

            await _db.ExecuteAsync(async db =>
            {
                var anyRemaining = await db.RequisitionDetails.AsNoTracking()
                    .AnyAsync(l => l.RequisitionId == info.RequisitionId && l.FulfilledQty < l.RequestedQty, ct);

                if (!anyRemaining)
                {
                    var req = await db.Requisitions.FirstOrDefaultAsync(r => r.RequisitionId == info.RequisitionId, ct);
                    if (req != null && req.Status == "Approved")
                    {
                        req.Status = "Fulfilled";
                        req.DecisionDate = DateTime.Now; 
                        await db.SaveChangesAsync(ct);
                    }
                }
            }, ct);

            return trxId;
        }

        private static void EnsureEditable(string status)
        {
            if (status is not ("Draft" or "Submitted"))
                throw new InvalidOperationException("لا يمكن تعديل الطلب في هذه الحالة.");
        }

        private static async Task<string> GenerateUniqueRequisitionNoAsync(HospitalInventoryDbContext db, CancellationToken ct)
        {
            for (int i = 0; i < 5; i++)
            {
                
                var no = $"REQ-{DateTime.Now:yyMMdd}-{Random.Shared.Next(10000, 99999)}";
                var exists = await db.Requisitions.AsNoTracking().AnyAsync(r => r.RequisitionNo == no, ct);
                if (!exists) return no;
            }

            throw new InvalidOperationException("Failed to generate a unique RequisitionNo.");
        }
    }
}