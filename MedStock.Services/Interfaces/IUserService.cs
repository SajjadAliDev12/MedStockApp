using System.Threading;
using System.Threading.Tasks;
using MedStock.Services.DTOs;

namespace MedStock.Services.Interfaces
{
    public interface IUserService
    {
        Task<SessionUser> AuthenticateAsync(string username, string password, CancellationToken ct = default);
    }
}
