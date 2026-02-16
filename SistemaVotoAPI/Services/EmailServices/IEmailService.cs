using SistemaVotoAPI.Models;
using System.Threading.Tasks;
namespace SistemaVotoAPI.Services.EmailServices
{
    public interface IEmailService
    {
        Task SendEmail(EmailDto request, byte[] attachment, string fileName);
    }
}
