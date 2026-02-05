using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MedStock.Services.DTOs;

namespace MedStock.Services.Interfaces
{
    public interface IDepartmentsService
    {
        Task<IReadOnlyList<DepartmentListRow>> GetAsync(string? search, bool includeInactive = true, CancellationToken ct = default);
        Task<DepartmentUpsertRequest> GetForEditAsync(int departmentId, CancellationToken ct = default);

        Task<int> SaveAsync(DepartmentUpsertRequest req, int userId, CancellationToken ct = default);
        Task SetActiveAsync(int departmentId, bool isActive, int userId, CancellationToken ct = default);
    }
}
