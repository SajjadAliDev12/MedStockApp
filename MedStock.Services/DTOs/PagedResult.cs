using System.Collections.Generic;

namespace MedStock.Services.DTOs
{
    // كلاس Generic يحمل أي قائمة بيانات مع معلومات التصفح
    public class PagedResult<T>
    {
        public IReadOnlyList<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }  // العدد الكلي في قاعدة البيانات
        public int PageNumber { get; set; }  // الصفحة الحالية
        public int PageSize { get; set; }    // حجم الصفحة
        public int TotalPages { get; set; }  // عدد الصفحات الكلي
    }
}