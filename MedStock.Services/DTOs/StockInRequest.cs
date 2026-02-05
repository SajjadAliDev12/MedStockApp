using System;
using System.Collections.Generic;

namespace MedStock.Services.DTOs
{
    public sealed class StockInRequest
    {
        public int CreatedByUserId { get; init; }
        public int? DepartmentId { get; init; } // optional
        public string ReasonCode { get; init; } = TransactionReasons.Purchase; // default IN reason
        public string? Notes { get; init; }
        public int? SupplierId { get; init; }
        public List<StockInLine> Lines { get; init; } = new();
    }

    public sealed class StockInLine
    {
        public int ItemId { get; init; }
        public string BatchCode { get; init; } = string.Empty;
        public DateOnly? ExpiryDate { get; init; }
        public DateOnly? ReceivedDate { get; init; }
        public string? SupplierRef { get; init; }
        public string? LocationCode { get; init; }
        public decimal Quantity { get; init; }       // > 0
        public decimal UnitCost { get; init; }       // >= 0
    }
}
