using SistemaVotoAPI.Models;
using System.Threading.Tasks;

namespace SistemaVotoAPI.Services.EmailServices
{
    public interface IEmailServices
    {
        Task SendEmail(EmailDto request);

    }
}
