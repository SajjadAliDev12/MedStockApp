using System.Collections.Generic;

namespace MedStock.Services.DTOs
{
    public sealed class UserListRow
    {
        public int UserId { get; init; }
        public string Username { get; init; } = "";
        public string DisplayName { get; init; } = "";
        public string Roles { get; init; } = ""; // Comma separated
        public bool IsActive { get; init; }
    }

    public sealed class UserUpsertRequest
    {
        public int? UserId { get; init; }
        public string Username { get; init; } = "";
        public string DisplayName { get; init; } = "";
        public string? Password { get; init; } // Required only for New
        public List<int> RoleIds { get; init; } = new();
        public bool IsActive { get; init; } = true;
    }
}