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
        // Navegacion
        public Eleccion? Eleccion { get; set; }
        public Lista? Lista { get; set; }
        public Candidato? Candidato { get; set; }
    }
}