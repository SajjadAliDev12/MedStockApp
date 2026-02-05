namespace MedStock.Services.DTOs
{
    public sealed class CategoryListRow
    {
        public int CategoryId { get; init; }
        public string Name { get; init; } = "";
        public bool IsActive { get; init; }
    }

    public sealed class CategoryUpsertRequest
    {
        public int? CategoryId { get; init; }
        public string Name { get; init; } = "";
        public bool IsActive { get; init; } = true;
    }
}
