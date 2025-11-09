using Microsoft.AspNetCore.Identity.UI.Services;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace OnlineTutor2.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailSender> _logger;

        public EmailSender(IConfiguration configuration, ILogger<EmailSender> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            try
            {
                var apiKey = _configuration["EmailSettings:SendGridApiKey"];

                if (string.IsNullOrEmpty(apiKey))
                {
                    throw new Exception("SendGrid API ключ не настроен");
                }

                var fromEmail = _configuration["EmailSettings:FromEmail"];
                var fromName = _configuration["EmailSettings:FromName"];

                _logger.LogInformation($"Отправка email через SendGrid на {email}");

                var client = new SendGridClient(apiKey);
                var from = new EmailAddress(fromEmail, fromName);
                var to = new EmailAddress(email);

                var msg = MailHelper.CreateSingleEmail(
                    from,
                    to,
                    subject,
                    null,
                    htmlMessage
                );

                var response = await client.SendEmailAsync(msg);

                if (response.StatusCode == System.Net.HttpStatusCode.Accepted ||
                    response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    _logger.LogInformation($"Email успешно отправлен на {email}");
                }
                else
                {
                    var body = await response.Body.ReadAsStringAsync();
                    _logger.LogError($"SendGrid вернул код: {response.StatusCode}");
                    _logger.LogError($"Ответ: {body}");
                    throw new Exception($"SendGrid ошибка: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка отправки email на {email}: {ex.Message}");
                _logger.LogError($"StackTrace: {ex.StackTrace}");
                throw new Exception($"Не удалось отправить email: {ex.Message}", ex);
            }
        }
    }
}
