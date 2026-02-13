using System;

namespace SistemaVotoAPI.Models
{
    public class CertificadoVotacionRequest
    {
        public string Nombre { get; set; } = string.Empty;
        public string Cedula { get; set; } = string.Empty;
        public string Provincia { get; set; } = string.Empty;
        public string Canton { get; set; } = string.Empty;
        public string Parroquia { get; set; } = string.Empty;
        public string FechaElectoral = DateTime.Now.ToString("dd 'DE' MMMM 'DE' yyyy").ToUpper();
        public string CertificadoNo = Random.Shared.Next(10_000_000, 100_000_000).ToString();

        public string RutaEscudo { get; set; } = string.Empty;
        public string RutaCne { get; set; } = string.Empty;
        public string RutaQr { get; set; } = string.Empty;
        public byte[] FotoBytes { get; set; }
        public string RutaFirma { get; set; } = string.Empty;
    }
}
