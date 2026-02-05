namespace MedStock.Services.DTOs
{
    public sealed class MinStockAlertRow
    {
        public int ItemId { get; init; }
        public string Name { get; init; } = "";
        public string? Sku { get; init; }
        public decimal? MinStock { get; init; }
        public decimal CurrentStock { get; init; }
    }
}
