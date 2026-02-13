using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using SistemaVotoAPI.Models;

namespace SistemaVotoAPI.Services.EmailServices
{
    public class EmailServices: IEmailService
    {
        private readonly IConfiguration _config;

        public EmailServices(IConfiguration config)
        {
            _config = config;

        }

        public void SendEmail(EmailDto request, byte[] attachment, string fileName)
        {
            var email = new MimeMessage();
            // Leemos el remitente desde tu appsettings.json
            email.From.Add(MailboxAddress.Parse(_config.GetSection("EmailUsername").Value));
            email.To.Add(MailboxAddress.Parse(request.To));
            email.Subject = request.Subject;

            // Usamos BodyBuilder para poder adjuntar archivos
            var builder = new BodyBuilder();
            builder.HtmlBody = request.Body;

            // Adjuntamos el PDF generado
            if (attachment != null && fileName != null)
            {
                builder.Attachments.Add(fileName, attachment);
            }

            email.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            smtp.Connect(_config.GetSection("EmailHost").Value, 587, SecureSocketOptions.StartTls);
            smtp.Authenticate(_config.GetSection("EmailUsername").Value, _config.GetSection("EmailPassword").Value);
            smtp.Send(email);
            smtp.Disconnect(true);
        }
    }
}
