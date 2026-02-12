using System;
using System.Collections.Generic;
using System.Text;

namespace SistemaVotoModelos.DTOs
{
    public class RegistroVotoDto
    {
        public string Cedula { get; set; } = "";
        public int EleccionId { get; set; }
        public List<DetalleVotoDto> Votos { get; set; } = new();
    }
}