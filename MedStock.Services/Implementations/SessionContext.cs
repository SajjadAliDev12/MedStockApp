using System;
using MedStock.Services.DTOs;
using MedStock.Services.Interfaces;

namespace MedStock.Services.Implementations
{
    public sealed class SessionContext : ISessionContext
    {
        public SessionUser? CurrentUser { get; private set; }
        public bool IsAuthenticated => CurrentUser != null;

        public event Action? SessionChanged;

        public void SetUser(SessionUser user)
        {
            CurrentUser = user ?? throw new ArgumentNullException(nameof(user));
            SessionChanged?.Invoke();
        }

        public void Clear()
        {
            CurrentUser = null;
            SessionChanged?.Invoke();
        }
    }
}
