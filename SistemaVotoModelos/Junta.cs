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
        public int DireccionId { get; set; }
        public Direccion? Direccion { get; set; }

        // El Jefe de esta mesa (Votante con RolId 3)
        public int JefeDeJuntaId { get; set; }
    }
}
