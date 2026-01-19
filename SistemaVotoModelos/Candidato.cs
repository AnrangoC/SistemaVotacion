using System.ComponentModel.DataAnnotations;

namespace SistemaVotoModelos
{
    public class Candidato : Votante
    {
        [Required]
        public int ListaId { get; set; }
        public Lista? Lista { get; set; }
        [Required]
        public string RolPostulante { get; set; } = string.Empty; // Ej: Presidente
    }
}