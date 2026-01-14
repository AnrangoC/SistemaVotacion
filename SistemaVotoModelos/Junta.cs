using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace SistemaVotoModelos
{
    public class Junta
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public int NumeroMesa { get; set; }

        // Ubicación de la mesa
        public int DireccionId { get; set; }
        public Direccion? Direccion { get; set; }

        // Cédula del Jefe de Mesa (Votante con RolId 3)
        public string JefeDeJuntaId { get; set; } = string.Empty;
    }
}
