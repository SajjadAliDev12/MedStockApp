using System.Threading;
using System.Threading.Tasks;

namespace MedStock.Services.Interfaces
{
    public interface IEmailService
    {
        bool IsConfigured { get; }

        Task<bool> SendAsync(string to, string subject, string body, CancellationToken ct = default);

        Task<bool> SendAlertsDigestAsync(string to, int minStock, int expiring, int pending, CancellationToken ct = default);
    }
}
