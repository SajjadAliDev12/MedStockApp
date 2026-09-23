using System.Collections.Generic;

namespace MedStock.Services.DTOs
{
    public sealed class SessionUser
    {
        public int UserId { get; init; }
        public string Username { get; init; } = "";
        public string DisplayName { get; init; } = "";
        public IReadOnlyList<string> Roles { get; init; } = new List<string>();

        public bool IsInRole(params string[] roleNames)
        {
            if (roleNames == null || roleNames.Length == 0) return false;
            if (Roles == null) return false;
            for (int i = 0; i < roleNames.Length; i++)
            {
                var wanted = roleNames[i];
                if (string.IsNullOrWhiteSpace(wanted)) continue;
                foreach (var r in Roles)
                {
                    if (string.Equals(r, wanted, System.StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
            return false;
        }
    }
}
