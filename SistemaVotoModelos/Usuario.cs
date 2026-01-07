using System.ComponentModel.DataAnnotations;

namespace SistemaVotoModelos
{
    public class Usuario
    {
        public int Id { get; set; }
        public string CedulaIdentidad { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public int RolId { get; set; }
        public int? BarrioId { get; set; }
        public Barrio? Barrio { get; set; }
        public string? DireccionExacta { get; set; }
        public bool YaVoto { get; set; } = false;
        public bool Activo { get; set; } = true;
        public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
    }
}