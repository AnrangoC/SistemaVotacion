using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace SistemaVotoModelos
{
    public class HistorialParticipacion
    {
        [Key]
        public int Id { get; set; }
        public DateTime FechaVoto { get; set; } = DateTime.UtcNow;
        // Navegacion
        public Eleccion? Eleccion { get; set; }
        public Usuario? Votante { get; set; }
    }
}