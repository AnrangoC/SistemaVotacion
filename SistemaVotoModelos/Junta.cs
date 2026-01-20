using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SistemaVotoModelos
{
    public class Junta
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public int NumeroMesa { get; set; }
        public int DireccionId { get; set; }
        public Direccion? Direccion { get; set; }
        [Required]
        public string JefeDeJuntaId { get; set; } = string.Empty;
        public Votante? JefeDeJunta { get; set; }
        // 1=Cerrada | 2=Abierta | 3=Pendiente | 4=Aprobada
        public int Estado { get; set; } = 1;
        // Relación: una junta tiene muchos votantes
        public ICollection<Votante> Votantes { get; set; } = new List<Votante>();
    }
}
