using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MedStock.Services.DTOs;

namespace MedStock.Services.Interfaces
{
    public interface IRequisitionsService
    {
        Task<IReadOnlyList<DepartmentRow>> GetDepartmentsAsync(CancellationToken ct = default);
        Task<IReadOnlyList<RequisitionListRow>> GetListAsync(string? status, int? departmentId, string? search, CancellationToken ct = default);

        Task<(RequisitionHeaderDto header, IReadOnlyList<RequisitionDetailRow> lines)> GetDetailsAsync(long requisitionId, CancellationToken ct = default);

        Task<long> CreateDraftAsync(int departmentId, string? notes, int requestedByUserId, CancellationToken ct = default);
        Task UpdateHeaderAsync(long requisitionId, int departmentId, string? notes, int userId, CancellationToken ct = default);

        Task AddOrUpdateLineAsync(long requisitionId, int itemId, decimal requestedQty, string? notes, int userId, CancellationToken ct = default);
        Task RemoveLineAsync(long requisitionDetailId, int userId, CancellationToken ct = default);
        Task<IReadOnlyList<PendingRequisitionRow>> GetPendingToIssueAsync(CancellationToken ct = default);

        Task SubmitAsync(long requisitionId, int userId, CancellationToken ct = default);
        Task ApproveAsync(long requisitionId, int approvedByUserId, CancellationToken ct = default);
        Task RejectAsync(long requisitionId, int rejectedByUserId, CancellationToken ct = default);
        Task CancelAsync(long requisitionId, int userId, CancellationToken ct = default);

        // Fulfill a single line (creates StockOut transaction + links)
        Task<long> FulfillLineAsync(long requisitionDetailId, decimal qtyToFulfill, int createdByUserId, int? departmentId, string? notes, CancellationToken ct = default);
    }
}
