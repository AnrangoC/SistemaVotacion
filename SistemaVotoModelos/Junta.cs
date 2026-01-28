using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaVotoModelos
{
    public class Junta
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Required]
        public int NumeroMesa { get; set; }
        [Required]
        public int DireccionId { get; set; }
        public Direccion? Direccion { get; set; }
        [Required]
        public int EleccionId { get; set; }
        public Eleccion? Eleccion { get; set; }

        // Puede ser null cuando no hay jefe asignado
        public string? JefeDeJuntaId { get; set; } = null;
        public Votante? JefeDeJunta { get; set; }
        // 1=Cerrada | 2=Abierta | 3=Pendiente de aprobación | 4=Aprobada
        public int Estado { get; set; } = 1;
        public ICollection<Votante> Votantes { get; set; } = new List<Votante>();
    }
}
