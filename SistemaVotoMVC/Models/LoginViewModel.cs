using System.ComponentModel.DataAnnotations;

namespace SistemaVotoMVC.Models
{
    public class LoginViewModel
    {
        public string Cedula { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
