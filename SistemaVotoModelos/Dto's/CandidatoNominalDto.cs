namespace SistemaVotoModelos.DTOs
{
    public class CandidatoNominalDto
    {
        public int Id { get; set; }
        public string Cedula { get; set; } = "";
        public string NombreCompleto { get; set; } = "";
        public string FotoUrl { get; set; } = "";
        public int ListaId { get; set; }
        public string NombreLista { get; set; } = "";
        public string RolPostulante { get; set; } = "";
    }
}
