using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace SistemaVotoModelos
{
    public class VotoAnonimo
    {
        [Key]
        public int Id { get; set; }
        public DateTime FechaVoto { get; set; } = DateTime.UtcNow;
        public int EleccionId { get; set; }
        public int DireccionId { get; set; } // Para reportes por zona
        public int NumeroMesa { get; set; }
        public int? ListaId { get; set; }
        public int? CandidatoId { get; set; }
    }
}