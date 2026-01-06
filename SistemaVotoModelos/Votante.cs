using System.ComponentModel.DataAnnotations;

namespace SistemaVotoModelos
{
    public class Votante
    {
        [Key]
        public int Id { get; set; } // Cambiado de Guid a int
        public string NombreCompleto { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string FotoUrl { get; set; } = string.Empty;
        public int RolId { get; set; } // 1: Admin, 2: Votante
        public bool Estado { get; set; } = true;
    }
}