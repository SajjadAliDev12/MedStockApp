namespace MedStock.Services.DTOs
{
    public sealed class RequisitionListRow
    {
        public long RequisitionId { get; init; }
        public string RequisitionNo { get; init; } = "";
        public string DepartmentName { get; init; } = "";
        public string Status { get; init; } = "";
        public System.DateTime RequestDate { get; init; }
        public string RequestedBy { get; init; } = "";
        public string? ApprovedBy { get; init; }
    }

    public sealed class RequisitionHeaderDto
    {
        public long RequisitionId { get; init; }
        public string RequisitionNo { get; init; } = "";
        public int DepartmentId { get; init; }
        public string Status { get; init; } = "";
        public System.DateTime RequestDate { get; init; }
        public System.DateTime? DecisionDate { get; init; }
        public string? Notes { get; init; }
        public int RequestedByUserId { get; init; }
        public int? ApprovedByUserId { get; init; }
    }

    public sealed class RequisitionDetailRow
    {
        public long RequisitionDetailId { get; init; }
        public int ItemId { get; init; }
        public string ItemName { get; init; } = "";
        public string Sku { get; init; } = "";
        public decimal RequestedQty { get; init; }
        public decimal FulfilledQty { get; init; }
        public decimal RemainingQty => RequestedQty - FulfilledQty;
        public string? Notes { get; init; }
        public bool IsFulfilled => FulfilledQty >= RequestedQty;
    }

    public sealed class DepartmentRow
    {
        public int DepartmentId { get; init; }
        public string DepartmentName { get; init; } = "";
    }
}
