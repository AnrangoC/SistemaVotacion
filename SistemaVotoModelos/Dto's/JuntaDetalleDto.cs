using System;

namespace SistemaVotoModelos.DTOs
{
    public class JuntaDetalleDto
    {
        public long Id { get; set; }
        public int NumeroMesa { get; set; }
        public int DireccionId { get; set; }
        public int EleccionId { get; set; }
        public string Ubicacion { get; set; } = string.Empty;
        public string NombreJefe { get; set; } = string.Empty;
        public int EstadoJunta { get; set; }

        public int TotalVotantes { get; set; }   
        public int Votaron { get; set; }        
        public int NoVotaron { get; set; }      
        public int VotosDigitales { get; set; }  
    }
}

