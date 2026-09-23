using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using MedStock.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace MedStock.Services.Implementations
{
    /// <summary>
    /// SendGrid email sender. Best-effort: never throws, returns false when
    /// not configured or on failure. No DI registration required — VMs build
    /// it via <see cref="FromConfiguration"/> from the Host-provided
    /// <see cref="IConfiguration"/> (Host.CreateDefaultBuilder registers
    /// IConfiguration automatically, so ctor injection just works).
    /// </summary>
    public sealed class EmailService : IEmailService
    {
        private readonly string _apiKey;
        private readonly string _fromEmail;
        private readonly string _fromName;

        public EmailService(IConfiguration config)
        {
            Guard.NotNull(config, nameof(config));
            _apiKey = config["Email:ApiKey"] ?? "";
            _fromEmail = config["Email:FromEmail"] ?? "";
            _fromName = config["Email:FromName"] ?? "MedStock";
        }

        public static IEmailService FromConfiguration(IConfiguration config) => new EmailService(config);

        public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);

        public async Task<bool> SendAsync(string to, string subject, string body, CancellationToken ct = default)
        {
            // Best-effort notification path: log + return false, never throw.
            if (!IsConfigured)
            {
                Debug.WriteLine("EmailService: skipped, Email:ApiKey is not configured.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(to))
            {
                Debug.WriteLine("EmailService: skipped, recipient is empty.");
                return false;
            }

            try
            {
                var client = new SendGridClient(_apiKey);
                var from = new EmailAddress(
                    string.IsNullOrWhiteSpace(_fromEmail) ? to.Trim() : _fromEmail.Trim(),
                    _fromName);
                var msg = MailHelper.CreateSingleEmail(
                    from,
                    new EmailAddress(to.Trim()),
                    subject ?? "",
                    body ?? "",
                    body ?? "");
                var response = await client.SendEmailAsync(msg, ct).ConfigureAwait(false);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"EmailService error: {ex.Message}");
                return false;
            }
        }

        public Task<bool> SendAlertsDigestAsync(string to, int minStock, int expiring, int pending, CancellationToken ct = default)
        {
            var subject = "ملخص تنبيهات المخزون — MedStock";
            var body = $"تنبيهات الحد الأدنى: {minStock}\nمواد قريبة الانتهاء (90 يوم): {expiring}\nطلبات معلقة: {pending}";
            return SendAsync(to, subject, body, ct);
        }
    }
}
