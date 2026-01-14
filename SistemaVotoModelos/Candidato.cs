using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
namespace SistemaVotoModelos
{
    public class Candidato : Votante
    {
        public int ListaId { get; set; }
        public Lista? Lista { get; set; }
        public string RolPostulante { get; set; } = string.Empty; // Ej: Presidente
    }
}