namespace MedStock.Services.DTOs
{
    public sealed class IdNameRow
    {
        public int Id { get; init; }
        public string Name { get; init; } = "";
        public bool IsActive { get; init; }
    }
}
