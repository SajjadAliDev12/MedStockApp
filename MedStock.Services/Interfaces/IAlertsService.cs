using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MedStock.Services.DTOs;

namespace MedStock.Services.Interfaces
{
    public interface IAlertsService
    {
        Task<IReadOnlyList<MinStockAlertRow>> GetMinStockAlertsAsync(string? search, CancellationToken ct = default);
    }
}
