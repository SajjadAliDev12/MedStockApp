namespace MedStock.Services.DTOs
{
    public sealed class SupplierListRow
    {
        public int SupplierId { get; init; }
        public string Name { get; init; } = "";
        public string? Phone { get; init; }
        public string? Email { get; init; }
        public bool IsActive { get; init; }
    }

    public sealed class SupplierUpsertRequest
    {
        public int? SupplierId { get; init; } // null = New
        public string Name { get; init; } = "";
        public string? Phone { get; init; }
        public string? Email { get; init; }
        public string? Address { get; init; }
        public bool IsActive { get; init; } = true;
    }
}