using MedStock.Services.DTOs;

namespace MedStock.Services.Interfaces
{
    public interface ISessionContext
    {
        SessionUser? CurrentUser { get; }
        bool IsAuthenticated { get; }

        void SetUser(SessionUser user);
        void Clear();
    }
}
