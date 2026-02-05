using MedStock.Data.Context;
using MedStock.Services.DTOs;
using MedStock.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MedStock.Services.Implementations
{
    public sealed class AuditService : IAuditService
    {
        private readonly DbExecutor _db;

        public AuditService(DbExecutor db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public Task<PagedResult<AuditLogListRow>> SearchLogsAsync(AuditLogFilter filter, CancellationToken ct = default)
        {
            return _db.ExecuteAsync<PagedResult<AuditLogListRow>>(async db =>
            {
                var query = db.AuditLogs.AsNoTracking()
                    .Include(x => x.User) // نحتاج لجدول المستخدمين لجلب الاسم
                    .AsQueryable();

                // 1. تصفية حسب التاريخ (إجباري أو اختياري حسب الحاجة)
                if (filter.FromDate.HasValue)
                    query = query.Where(x => x.OccurredAt >= filter.FromDate.Value);

                if (filter.ToDate.HasValue)
                {
                    // نأخذ حتى نهاية اليوم المحدد
                    var to = filter.ToDate.Value.Date.AddDays(1).AddTicks(-1);
                    query = query.Where(x => x.OccurredAt <= to);
                }

                // 2. تصفية حسب المستخدم
                if (filter.UserId.HasValue && filter.UserId.Value > 0)
                    query = query.Where(x => x.UserId == filter.UserId.Value);

                // 3. تصفية حسب نوع الحركة
                if (!string.IsNullOrWhiteSpace(filter.ActionType))
                    query = query.Where(x => x.ActionType == filter.ActionType);

                // 4. تصفية حسب الجدول/الكيان
                if (!string.IsNullOrWhiteSpace(filter.EntityName))
                    query = query.Where(x => x.EntityName == filter.EntityName);

                // 5. بحث نصي في الملخص
                if (!string.IsNullOrWhiteSpace(filter.SearchText))
                {
                    var txt = filter.SearchText.Trim();
                    query = query.Where(x => x.Summary.Contains(txt) || x.EntityId.Contains(txt));
                }

                // 6. حساب العدد الكلي (Total Count) قبل القص
                // هذا الاستعلام سريع جداً لأنه يعيد رقماً فقط ولا يجلب البيانات
                var totalCount = await query.CountAsync(ct);

                // 7. جلب بيانات الصفحة الحالية فقط (Pagination)
                // دائماً الأحدث أولاً
                var items = await query
                    .OrderByDescending(x => x.OccurredAt)
                    .Skip((filter.PageNumber - 1) * filter.PageSize) // تجاوز الصفحات السابقة
                    .Take(filter.PageSize)                           // خذ عدد الصفحة الحالية فقط
                    .Select(x => new AuditLogListRow
                    {
                        AuditLogId = x.AuditLogId,
                        OccurredAt = x.OccurredAt,
                        UserName = x.User != null ? x.User.DisplayName : "System/Unknown",
                        ActionType = x.ActionType,
                        EntityName = x.EntityName,
                        EntityId = x.EntityId,
                        Summary = x.Summary,
                        DetailsJson = x.DetailsJson,
                        MachineName = x.MachineName,
                        IpAddress = x.IpAddress
                    })
                    .ToListAsync(ct);

                // 8. إرجاع النتيجة المغلفة مع معلومات الصفحات
                return new PagedResult<AuditLogListRow>
                {
                    Items = items,
                    TotalCount = totalCount,
                    PageNumber = filter.PageNumber,
                    PageSize = filter.PageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize)
                };

            }, ct);
        }

        public Task<IReadOnlyList<IdNameRow>> GetUsersLookupAsync(CancellationToken ct = default)
        {
            return _db.ExecuteAsync<IReadOnlyList<IdNameRow>>(async db =>
            {
                // نجلب فقط المستخدمين الذين لديهم نشاط في سجل التدقيق (لتقليل القائمة)
                // أو نجلب كل المستخدمين حسب رغبتك. هنا سنجلب الجميع.
                return await db.Users.AsNoTracking()
                    .Where(u => u.IsActive)
                    .OrderBy(u => u.DisplayName)
                    .Select(u => new IdNameRow { Id = u.UserId, Name = u.DisplayName })
                    .ToListAsync(ct);
            }, ct);
        }

        public Task<IReadOnlyList<string>> GetActionTypesAsync(CancellationToken ct = default)
        {
            return _db.ExecuteAsync<IReadOnlyList<string>>(async db =>
            {
                // نجلب أنواع الحركات الموجودة فعلياً في قاعدة البيانات
                return await db.AuditLogs.AsNoTracking()
                    .Select(a => a.ActionType)
                    .Distinct()
                    .OrderBy(a => a)
                    .ToListAsync(ct);
            }, ct);
        }
    }
}