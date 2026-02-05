using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MedStock.Services.DTOs;

namespace MedStock.Services.Interfaces
{
    public interface ICategoriesService
    {
        Task<IReadOnlyList<CategoryListRow>> GetAsync(string? search, CancellationToken ct = default);
        Task<int> SaveAsync(CategoryUpsertRequest req, int userId, CancellationToken ct = default);
        Task SetActiveAsync(int categoryId, bool isActive, int userId, CancellationToken ct = default);
    }
}
