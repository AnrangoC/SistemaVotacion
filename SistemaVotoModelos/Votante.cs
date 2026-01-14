using System.ComponentModel.DataAnnotations;

namespace SistemaVotoModelos
{
    public class Votante
    {
        [Key]
        public string Cedula { get; set; } = string.Empty; // ID único
        [Required]
        public string NombreCompleto { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FotoUrl { get; set; } = string.Empty;
        public int RolId { get; set; } // 1: Admin, 2: Votante, 3: Jefe de Junta
        public bool Estado { get; set; } = true;
        public bool HaVotado { get; set; } = false;

        // Relación crucial: Aquí se sabe en qué mesa vota
        public int? JuntaId { get; set; }
        public Junta? Junta { get; set; }
    }
}
