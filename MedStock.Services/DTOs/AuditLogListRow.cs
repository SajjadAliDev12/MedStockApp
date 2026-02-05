using System;

namespace MedStock.Services.DTOs
{
    // 1. الصف الذي سيظهر في الجدول (Grid)
    public class AuditLogListRow
    {
        public long AuditLogId { get; set; }
        public DateTime OccurredAt { get; set; }
        public string UserName { get; set; } = "";
        public string ActionType { get; set; } = "";
        public string EntityName { get; set; } = "";
        public string EntityId { get; set; } = "";
        public string Summary { get; set; } = "";
        public string? DetailsJson { get; set; }
        public string? MachineName { get; set; }
        public string? IpAddress { get; set; }
    }

    // 2. كلاس الفلترة بعد التنظيف
    public class AuditLogFilter
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? UserId { get; set; }
        public string? ActionType { get; set; }
        public string? EntityName { get; set; }
        public string? SearchText { get; set; }

        // إعدادات الصفحات فقط
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 50; // هذا هو الـ Take الجديد لكل صفحة
    }
}