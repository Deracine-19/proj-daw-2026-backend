using System.Net;
using System.Net.Mail;

namespace proj_daw_2026_backend.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string bodyHtml)
        {
            var smtpServer = _config["SmtpSettings:Server"];
            var port = int.Parse(_config["SmtpSettings:Port"] ?? "2525");
            var senderEmail = _config["SmtpSettings:SenderEmail"];
            var senderName = _config["SmtpSettings:SenderName"];
            var username = _config["SmtpSettings:Username"];
            var password = _config["SmtpSettings:Password"];

            using var message = new MailMessage
            {
                From = new MailAddress(senderEmail!, senderName),
                Subject = subject,
                Body = bodyHtml,
                IsBodyHtml = true
            };

            message.To.Add(new MailAddress(toEmail));

            using var client = new SmtpClient(smtpServer, port)
            {
                Credentials = new NetworkCredential(username, password),
                EnableSsl = true
            };

            await client.SendMailAsync(message);
        }
    }
}