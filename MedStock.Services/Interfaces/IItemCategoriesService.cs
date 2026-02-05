using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MedStock.Services.DTOs;

namespace MedStock.Services.Interfaces
{
    public interface IItemCategoriesService
    {
        Task<IReadOnlyList<IdNameRow>> GetItemsAsync(string? search, CancellationToken ct = default);
        Task<IReadOnlyList<IdNameRow>> GetCategoriesAsync(CancellationToken ct = default);

        Task<IReadOnlySet<int>> GetAssignedCategoryIdsAsync(int itemId, CancellationToken ct = default);
        Task SetAssignedCategoriesAsync(int itemId, IReadOnlyCollection<int> categoryIds, int userId, CancellationToken ct = default);
    }
}
