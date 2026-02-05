namespace MedStock.Services.DTOs
{
    // للعرض في جدول شاشة الأقسام
    public sealed class DepartmentListRow
    {
        public int DepartmentId { get; init; }
        public string? Code { get; init; }
        public string Name { get; init; } = "";
        public string? Notes { get; init; }
        public bool IsActive { get; init; }
    }

    // للإضافة/التعديل
    public sealed class DepartmentUpsertRequest
    {
        public int? DepartmentId { get; init; }
        public string? Code { get; init; }
        public string Name { get; init; } = "";
        public string? Notes { get; init; }
        public bool IsActive { get; init; } = true;
    }
}
