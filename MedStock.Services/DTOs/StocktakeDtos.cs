using System;

namespace MedStock.Services.DTOs
{
    public sealed class StocktakeListRow
    {
        public int StocktakeId { get; init; }
        public string StocktakeNo { get; init; } = "";
        public DateTime Date { get; init; }
        public string Status { get; init; } = ""; // Draft, Posted, Cancelled
        public string CreatedBy { get; init; } = "";
        public string? Notes { get; init; }
    }

    public sealed class StocktakeItemRow
    {
        public long DetailId { get; init; }
        public int ItemId { get; init; }
        public string ItemName { get; init; } = "";
        public string Sku { get; init; } = "";
        public string Unit { get; init; } = "";

        public decimal SystemQty { get; init; }   // رصيد الكمبيوتر عند التجميد
        public decimal? PhysicalQty { get; set; } // رصيد العد الفعلي (قابل للتعديل)

        // خاصية للقراءة فقط تحسب الفرق
        public decimal Difference => (PhysicalQty ?? SystemQty) - SystemQty;
    }
}