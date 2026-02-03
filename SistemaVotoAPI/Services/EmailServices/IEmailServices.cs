using SistemaVotoAPI.Models;

namespace SistemaVotoAPI.Services.EmailServices
{
    public interface IEmailServices
    {
        void SendEmail(EmailDto request);

    }
}
