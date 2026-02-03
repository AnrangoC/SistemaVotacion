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

        // El token se usa solo para ingresar al sistema en el cliente MVC
        [HttpPost("Emitir")]
        public async Task<IActionResult> Emitir([FromBody] EmitirVotoRequestDto request)
        {
            if (request == null)
                return BadRequest("Datos inválidos.");

            var cedula = (request.Cedula ?? "").Trim();

            if (string.IsNullOrWhiteSpace(cedula))
                return BadRequest("Debe enviar cédula.");

            if (request.EleccionId <= 0)
                return BadRequest("EleccionId inválido.");

            // 1. Validar que el votante existe, está activo y no ha votado aún
            var votante = await _context.Votantes.FindAsync(cedula);
            if (votante == null || votante.Estado != true)
                return Unauthorized("Votante no existe o está inactivo.");

            if (votante.HaVotado)
                return Conflict("El votante ya registró su voto.");

            // 2. Validar que la elección exista y su estado sea estrictamente "ACTIVA"
            var eleccion = await _context.Elecciones.FindAsync(request.EleccionId);
            if (eleccion == null)
                return BadRequest("La elección no existe.");

            if (eleccion.Estado != "ACTIVA")
                return BadRequest("La elección no está habilitada para recibir votos en este momento.");

            // 3. Validar la existencia y el estado de la junta asignada
            if (!votante.JuntaId.HasValue)
                return BadRequest("El votante no tiene una junta asignada.");

            var junta = await _context.Juntas
                .FirstOrDefaultAsync(j => j.Id == votante.JuntaId.Value);

            if (junta == null)
                return BadRequest("No se encontró la junta del votante.");

            // 4. Crear el registro de voto anónimo
            var voto = new VotoAnonimo
            {
                // Uso de hora local de Ecuador para consistencia con EleccionesController
                FechaVoto = DateTime.Now,
                EleccionId = request.EleccionId,
                DireccionId = junta.DireccionId,
                NumeroMesa = junta.NumeroMesa,

                ListaId = request.ListaId, // 0 indica voto en blanco
                CedulaCandidato = (request.CedulaCandidato ?? "").Trim(),
                RolPostulante = (request.RolPostulante ?? "").Trim()
            };

            // 5. Marcar al votante para impedir que vote nuevamente y guardar cambios
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