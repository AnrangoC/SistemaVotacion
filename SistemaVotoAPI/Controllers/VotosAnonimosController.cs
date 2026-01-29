using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaVotoAPI.Data;
using SistemaVotoModelos;
using System;
using System.Threading.Tasks;

namespace SistemaVotoAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VotosAnonimosController : ControllerBase
    {
        private readonly APIVotosDbContext _context;

        public VotosAnonimosController(APIVotosDbContext context)
        {
            _context = context;
        }

        // el token se usa solo para ingresar
        [HttpPost("Emitir")]
        public async Task<IActionResult> Emitir([FromBody] EmitirVotoRequestDto request)
        {
            if (request == null)
                return BadRequest("Datos inválidos.");

            var cedula = (request.Cedula ?? "").Trim();

            if (string.IsNullOrWhiteSpace(cedula))
                return BadRequest("Debe enviar cedula.");

            if (request.EleccionId <= 0)
                return BadRequest("EleccionId inválido.");

            var votante = await _context.Votantes.FindAsync(cedula);
            if (votante == null || votante.Estado != true)
                return Unauthorized("Votante no existe o inactivo.");

            if (votante.HaVotado)
                return Conflict("El votante ya votó.");

            // Validar que la elección exista y esté ACTIVA
            var eleccion = await _context.Elecciones.FindAsync(request.EleccionId);
            if (eleccion == null)
                return BadRequest("Elección no existe.");

            if (eleccion.Estado != "ACTIVA")
                return BadRequest("La elección no está activa.");

            // Junta solo para DireccionId y NumeroMesa (reportes)
            if (!votante.JuntaId.HasValue)
                return BadRequest("El votante no tiene junta asignada.");

            var junta = await _context.Juntas
                .AsNoTracking()
                .FirstOrDefaultAsync(j => j.Id == votante.JuntaId.Value);

            if (junta == null)
                return BadRequest("No se encontró la junta del votante.");

            var voto = new VotoAnonimo
            {
                FechaVoto = DateTime.UtcNow,
                EleccionId = request.EleccionId,
                DireccionId = junta.DireccionId,
                NumeroMesa = junta.NumeroMesa,

                ListaId = request.ListaId, // 0 si voto en blanco
                CedulaCandidato = (request.CedulaCandidato ?? "").Trim(),
                RolPostulante = (request.RolPostulante ?? "").Trim()
            };

            // Marcar que ya votó 
            votante.HaVotado = true;

            _context.VotosAnonimos.Add(voto);
            await _context.SaveChangesAsync();

            return Ok("Voto registrado correctamente.");
        }
    }

    public class EmitirVotoRequestDto
    {
        public string Cedula { get; set; } = string.Empty;
        public int EleccionId { get; set; }

        public int ListaId { get; set; }               // 0 si voto en blanco
        public string? CedulaCandidato { get; set; }   // opcional
        public string? RolPostulante { get; set; }     // opcional
    }
}
