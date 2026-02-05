namespace MedStock.Services.DTOs
{
    public class PendingRequisitionRow
    {
        public long RequisitionDetailId { get; set; }
        public string RequisitionNo { get; set; } = "";
        public string DepartmentName { get; set; } = "";
        public int ItemId { get; set; }
        public string ItemName { get; set; } = "";
        public decimal RequestedQty { get; set; }
        public decimal FulfilledQty { get; set; }
        public decimal RemainingQty { get; set; }
        public string? Notes { get; set; }
    }
}