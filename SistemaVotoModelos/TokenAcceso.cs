using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace SistemaVotoModelos
{
    public class TokenAcceso
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [StringLength(6)]
        public string Codigo { get; set; } = string.Empty;
        public string VotanteId { get; set; } = string.Empty; // Cédula vinculada
        public bool EsValido { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    }
}
