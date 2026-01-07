using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace SistemaVotoModelos
{
    public class VotoAnonimo
    {
        public int Id { get; set; }
        public int EleccionId { get; set; }
        public int? ListaId { get; set; }
        public int? CandidatoId { get; set; }

        // Ubicación para estadísticas HU09
        public int? ProvinciaId { get; set; }
        public int? CantonId { get; set; }
        public int? ParroquiaId { get; set; }

        public DateTime FechaVoto { get; set; } = DateTime.UtcNow;
        public string FirmaDigital { get; set; } = string.Empty;
    }
}