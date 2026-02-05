namespace MedStock.Services.DTOs
{
    public sealed class ItemListRow
    {
        public int ItemId { get; init; }
        public string Name { get; init; } = "";
        public string Sku { get; init; } = "";
        public string UnitOfMeasure { get; init; } = "";
        public decimal ReorderLevel { get; init; }
        public decimal? MinStock { get; init; }
        public bool IsActive { get; init; }
        public decimal TotalQuantity { get; set; }
    }

    public sealed class ItemUpsertRequest
    {
        public int? ItemId { get; init; } // null => create

        public string Name { get; init; } = "";
        public string Sku { get; init; } = "";
        public string UnitOfMeasure { get; init; } = "";

        public decimal ReorderLevel { get; init; }
        public decimal? MinStock { get; init; }

        public string? Description { get; init; }
        public bool IsActive { get; init; } = true;
    }
    public sealed class ItemBatchRow
    {
        public long BatchId { get; init; }
        public string BatchCode { get; init; } = "";
        public decimal CurrentQty { get; init; }
        public System.DateOnly? ExpiryDate { get; init; }
        public string? LocationCode { get; init; } // المكان
        public decimal UnitCost { get; init; }
    }
}
