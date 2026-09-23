namespace MedStock.Services.DTOs
{
    public sealed class InventoryValueRow
    {
        public int ItemId { get; init; }
        public string ItemName { get; init; } = "";
        public string Sku { get; init; } = "";
        public string Unit { get; init; } = "";
        public decimal Qty { get; init; }
        public decimal AvgCost { get; init; }
        public decimal TotalValue { get; init; }
    }
}
