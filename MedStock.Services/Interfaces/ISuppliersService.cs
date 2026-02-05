using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MedStock.Services.DTOs;

namespace MedStock.Services.Interfaces
{
    public interface ISuppliersService
    {
        Task<IReadOnlyList<SupplierListRow>> GetListAsync(string? search, CancellationToken ct = default);
        Task<int> SaveAsync(SupplierUpsertRequest request, int userId, CancellationToken ct = default);
        Task ToggleActiveAsync(int supplierId, int userId, CancellationToken ct = default);

        // سنحتاجها لاحقاً للقوائم المنسدلة
        Task<IReadOnlyList<IdNameRow>> GetLookupAsync(CancellationToken ct = default);
    }
}