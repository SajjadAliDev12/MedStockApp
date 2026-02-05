using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MedStock.Services.DTOs;

namespace MedStock.Services.Interfaces
{
    public interface IStocktakeService
    {
        // القائمة الرئيسية
        Task<IReadOnlyList<StocktakeListRow>> GetListAsync(CancellationToken ct = default);

        // إنشاء جرد جديد (يقوم بتجميد الأرصدة)
        Task<int> CreateDraftAsync(int createdByUserId, string? notes, CancellationToken ct = default);

        // جلب تفاصيل الجرد للعد
        Task<IReadOnlyList<StocktakeItemRow>> GetDetailsAsync(int stocktakeId, CancellationToken ct = default);

        // حفظ العد الفعلي (بدون ترحيل)
        Task SaveCountsAsync(int stocktakeId, Dictionary<long, decimal> counts, CancellationToken ct = default);

        // ترحيل الجرد (تسوية الفروقات نهائياً)
        Task PostAsync(int stocktakeId, int postedByUserId, CancellationToken ct = default);

        // إلغاء الجرد
        Task CancelAsync(int stocktakeId, int userId, CancellationToken ct = default);
    }
}