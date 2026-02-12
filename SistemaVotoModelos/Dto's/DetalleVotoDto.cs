using System;
using System.Collections.Generic;
using System.Text;

namespace SistemaVotoModelos.DTOs
{
    public class DetalleVotoDto
    {
        public int ListaId { get; set; }
        public string? CedulaCandidato { get; set; }
        public string? RolPostulante { get; set; }
    }
}