using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MedStock.Services.DTOs;

namespace MedStock.Services.Interfaces
{
    public interface IAuditService
    {
        // البحث وجلب السجلات
        Task<PagedResult<AuditLogListRow>> SearchLogsAsync(AuditLogFilter filter, CancellationToken ct = default);

        // دوال مساعدة لملء الـ ComboBox في شاشة البحث
        Task<IReadOnlyList<IdNameRow>> GetUsersLookupAsync(CancellationToken ct = default);
        Task<IReadOnlyList<string>> GetActionTypesAsync(CancellationToken ct = default);
       
    }
}