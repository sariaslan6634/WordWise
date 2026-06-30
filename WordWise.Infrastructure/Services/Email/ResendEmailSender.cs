using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;
using WordWise.Application.Common.Settings;
using IEmailSender = WordWise.Application.Common.Interfaces.IEmailSender;

namespace WordWise.Infrastructure.Services.Email
{
    public class ResendEmailSender : IEmailSender
    {
        private readonly HttpClient _client;
        private readonly EmailSettings _settings;
        private readonly ILogger<ResendEmailSender> _logger;
        private const string ResendApiUrl = "https://api.resend.com/emails";

        public ResendEmailSender(
            HttpClient httpClient,
            IOptions<EmailSettings> options,
            ILogger<ResendEmailSender> logger)
        {
            _client = httpClient;
            _settings = options.Value;
            _logger = logger;

            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _settings.ApiKey);
        }

        public async Task SendAsync(string toEmail, string Subject, string htmlBody, CancellationToken cancellationToken = default)
        {
            var payload = new
            {
                from = _settings.FromAddress,
                to = new[] { toEmail },
                Subject,
                html = htmlBody
            };
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await _client.PostAsync(ResendApiUrl, content,cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("E-posta gönderimi başarısız oldu. Durum: {StatusCode}, Gövde: {ErrorBody}, Alıcı: {ToEmail}", response.StatusCode, errorBody, toEmail);
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{ToEmail} adresine e-posta gönderilirken beklenmeyen bir hata oluştu",toEmail);
            }
        }
    }
}
