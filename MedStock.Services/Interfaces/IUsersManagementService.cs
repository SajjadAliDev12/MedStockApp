using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MedStock.Services.DTOs;

namespace MedStock.Services.Interfaces
{
    public interface IUsersManagementService
    {
        Task<IReadOnlyList<UserListRow>> GetListAsync(string? search, CancellationToken ct = default);
        Task<IReadOnlyList<IdNameRow>> GetRolesAsync(CancellationToken ct = default);
        Task<int> SaveAsync(UserUpsertRequest request, int modifiedByUserId, CancellationToken ct = default);
        Task ToggleActiveAsync(int userId, int modifiedByUserId, CancellationToken ct = default);
        Task ResetPasswordAsync(int userId, string newPassword, int modifiedByUserId, CancellationToken ct = default);
    }
}