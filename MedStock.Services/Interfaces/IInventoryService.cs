using System.Threading;
using System.Threading.Tasks;
using MedStock.Services.DTOs;

namespace MedStock.Services.Interfaces
{
    public interface IInventoryService
    {
        Task<long> StockInAsync(StockInRequest request, CancellationToken ct = default);
        Task<List<TransactionReasonDto>> GetReasonsAsync(string scope);
        Task<long> StockOutAsync(StockOutRequest request, CancellationToken ct = default);
    }
}
