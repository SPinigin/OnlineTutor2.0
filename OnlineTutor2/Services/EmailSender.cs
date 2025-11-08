using Microsoft.AspNetCore.Identity.UI.Services;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;

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
                var smtpServer = _configuration["EmailSettings:SmtpServer"];
                var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"]);
                var smtpUsername = _configuration["EmailSettings:SmtpUsername"];
                var smtpPassword = _configuration["EmailSettings:SmtpPassword"];
                var fromEmail = _configuration["EmailSettings:FromEmail"];
                var fromName = _configuration["EmailSettings:FromName"];

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(fromName, fromEmail));
                message.To.Add(new MailboxAddress("", email));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = htmlMessage
                };
                message.Body = bodyBuilder.ToMessageBody();

                using (var client = new SmtpClient())
                {
                    // Подключаемся к SMTP серверу
                    await client.ConnectAsync(smtpServer, smtpPort, SecureSocketOptions.StartTls);

                    // Аутентификация
                    await client.AuthenticateAsync(smtpUsername, smtpPassword);

                    // Отправка
                    await client.SendAsync(message);

                    // Отключаемся
                    await client.DisconnectAsync(true);

                    _logger.LogInformation($"Email успешно отправлен на {email}");
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
