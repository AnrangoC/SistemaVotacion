using SistemaVotoAPI.Models;

namespace SistemaVotoAPI.Services.EmailServices
{
    public interface IEmailService
    {
        void SendEmail(EmailDto request, byte[] attachment, string fileName);
    }
}
