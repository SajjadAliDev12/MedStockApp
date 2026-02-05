using System;

namespace MedStock.Services.DTOs
{
    public sealed class StockOutRequest
    {
        public int CreatedByUserId { get; init; }
        public int? DepartmentId { get; init; } // optional but recommended for hospital flows

        public int ItemId { get; init; }
        public decimal Quantity { get; init; } // > 0

        // If fulfilling a requisition, provide RequisitionDetailId
        public long? RequisitionDetailId { get; init; }

        // OUT reason - if RequisitionDetailId is set, we enforce REQ_FULFILL unless you override intentionally
        public string ReasonCode { get; init; } = TransactionReasons.ReqFulfill;
        public string? Notes { get; init; }
    }
}
