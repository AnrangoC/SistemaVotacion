using System;

namespace SistemaVotoModelos.DTOs;

public class JuntaDetalleDto
{
    public int Id { get; set; }
    public int NumeroMesa { get; set; }

    public int DireccionId { get; set; }
    public int EleccionId { get; set; }

    public string Ubicacion { get; set; } = string.Empty; // Ej: "Imbabura - Ibarra - Alpachaca"
    public string NombreJefe { get; set; } = string.Empty;

    // Estados Junta (int)
    // 1=Cerrada | 2=Abierta | 3=Pendiente de aprobación | 4=Aprobada
    public int EstadoJunta { get; set; }
}
