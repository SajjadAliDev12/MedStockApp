using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MedStock.Services.DTOs;

namespace MedStock.Services.Interfaces
{
    public interface IItemsService
    {
        Task<IReadOnlyList<ItemListRow>> GetItemsAsync(string? search, CancellationToken ct = default);
        Task<ItemUpsertRequest> GetForEditAsync(int itemId, CancellationToken ct = default);
        Task<int> SaveAsync(ItemUpsertRequest request, int userId, CancellationToken ct = default);
        Task SetActiveAsync(int itemId, bool isActive, int userId, CancellationToken ct = default);
        Task<IReadOnlyList<ItemBatchRow>> GetBatchesAsync(int itemId, CancellationToken ct = default);

        // >>>> الإضافة الجديدة <<<<
        Task DeleteAsync(int itemId, int userId, CancellationToken ct = default);
        // استبدل الدالة القديمة GetListAsync أو أضف هذه الجديدة
        Task<PagedResult<ItemListRow>> SearchAsync(ItemFilter filter, CancellationToken ct = default);
    }
}