using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
namespace SistemaVotoModelos
{
    public class Candidato
    {
        [Key]
        public int Id { get; set; }
        public string Cargo { get; set; } = string.Empty;
        // Navegacion: Un candidato "es" un votante
        public Votante? DatosVotante { get; set; }
        // Navegacion: El candidato pertenece a una lista
        public Lista? Lista { get; set; }
    }
}