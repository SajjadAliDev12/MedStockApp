using MedStock.Services.DTOs;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MedStock.Services.Interfaces
{
    public interface IReportsService
    {
        // 1. تقرير بطاقة المادة (القديم)
        Task<IReadOnlyList<StockCardRow>> GetStockCardAsync(int itemId, DateTime? from, DateTime? to, CancellationToken ct = default);

        // 2. تقرير صلاحية المواد (الجديد)
        Task<IReadOnlyList<ExpiryReportRow>> GetExpiryReportAsync(int daysThreshold, CancellationToken ct = default);
        // أضف هذه الأسطر داخل الـ interface
        Task<IReadOnlyList<ConsumptionSummaryRow>> GetConsumptionSummaryAsync(DateTime? from, DateTime? to, int? categoryId, int? itemId, CancellationToken ct = default);

        Task<IReadOnlyList<ConsumptionDetailRow>> GetConsumptionDetailsAsync(int itemId, DateTime? from, DateTime? to, CancellationToken ct = default);
    }
}