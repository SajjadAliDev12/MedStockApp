using System;

namespace MedStock.Services.DTOs
{
    public class StockCardRow
    {
        public DateTime Date { get; init; }
        public string TransactionType { get; init; } = "";
        public string ReferenceNo { get; init; } = "";
        public decimal InQty { get; init; }
        public decimal OutQty { get; init; }
        public decimal Balance { get; set; }
        public string? Notes { get; init; }
    }
}