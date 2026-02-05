using System.Collections.Generic;

namespace MedStock.Services.DTOs
{
    public sealed class SessionUser
    {
        public int UserId { get; init; }
        public string Username { get; init; } = "";
        public string DisplayName { get; init; } = "";
        public IReadOnlyList<string> Roles { get; init; } = new List<string>();
    }
}
