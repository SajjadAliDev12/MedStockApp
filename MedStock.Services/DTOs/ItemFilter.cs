public class ItemFilter
{
    public string? SearchText { get; set; }
    public int? CategoryId { get; set; } // إذا كنت ستضيف فلتر للتصنيف لاحقاً
    public bool? IsActive { get; set; }  // null = الكل، true = فعال، false = غير فعال

    // إعدادات الصفحات
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}