using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaVotoAPI.Data;
using SistemaVotoModelos;
using System;
using System.Collections.Generic;
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

        // Registra un solo voto (se mantiene por compatibilidad)
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

            var votante = await _context.Votantes.FindAsync(cedula);
            if (votante == null || votante.Estado != true)
                return Unauthorized("Votante no existe o está inactivo.");

            if (votante.HaVotado)
                return Conflict("El votante ya registró su voto.");

            var eleccion = await _context.Elecciones.FindAsync(request.EleccionId);
            if (eleccion == null)
                return BadRequest("La elección no existe.");

            if (eleccion.Estado != "ACTIVA")
                return BadRequest("La elección no está habilitada.");

            if (!votante.JuntaId.HasValue)
                return BadRequest("El votante no tiene junta asignada.");

            var junta = await _context.Juntas
                .FirstOrDefaultAsync(j => j.Id == votante.JuntaId.Value);

            if (junta == null)
                return BadRequest("No se encontró la junta.");

            var voto = new VotoAnonimo
            {
                FechaVoto = DateTime.Now,
                EleccionId = request.EleccionId,
                DireccionId = junta.DireccionId,
                NumeroMesa = junta.NumeroMesa,
                ListaId = request.ListaId,
                CedulaCandidato = (request.CedulaCandidato ?? "").Trim(),
                RolPostulante = (request.RolPostulante ?? "").Trim()
            };

            votante.HaVotado = true;

            _context.VotosAnonimos.Add(voto);
            await _context.SaveChangesAsync();

            return Ok("Voto registrado correctamente.");
        }

        // Registra varios votos en una sola operación
        [HttpPost("EmitirMultiple")]
        public async Task<IActionResult> EmitirMultiple([FromBody] EmitirMultipleRequestDto request)
        {
            if (request == null || request.Votos == null || request.Votos.Count == 0)
                return BadRequest("No se recibieron votos.");

            var cedula = (request.Cedula ?? "").Trim();

            if (string.IsNullOrWhiteSpace(cedula))
                return BadRequest("Debe enviar cédula.");

            if (request.EleccionId <= 0)
                return BadRequest("EleccionId inválido.");

            var votante = await _context.Votantes.FindAsync(cedula);
            if (votante == null || votante.Estado != true)
                return Unauthorized("Votante no existe o está inactivo.");

            if (votante.HaVotado)
                return Conflict("El votante ya registró su voto.");

            var eleccion = await _context.Elecciones.FindAsync(request.EleccionId);
            if (eleccion == null)
                return BadRequest("La elección no existe.");

            if (eleccion.Estado != "ACTIVA")
                return BadRequest("La elección no está habilitada.");

            if (!votante.JuntaId.HasValue)
                return BadRequest("El votante no tiene junta asignada.");

            var junta = await _context.Juntas
                .FirstOrDefaultAsync(j => j.Id == votante.JuntaId.Value);

            if (junta == null)
                return BadRequest("No se encontró la junta.");

            foreach (var v in request.Votos)
            {
                var voto = new VotoAnonimo
                {
                    FechaVoto = DateTime.Now,
                    EleccionId = request.EleccionId,
                    DireccionId = junta.DireccionId,
                    NumeroMesa = junta.NumeroMesa,
                    ListaId = v.ListaId,
                    CedulaCandidato = (v.CedulaCandidato ?? "").Trim(),
                    RolPostulante = (v.RolPostulante ?? "").Trim()
                };

                _context.VotosAnonimos.Add(voto);
            }

            votante.HaVotado = true;

            await _context.SaveChangesAsync();

            return Ok("Votos registrados correctamente.");
        }
    }

    // DTO para voto individual
    public class EmitirVotoRequestDto
    {
        public string Cedula { get; set; } = "";
        public int EleccionId { get; set; }
        public int ListaId { get; set; }
        public string? CedulaCandidato { get; set; }
        public string? RolPostulante { get; set; }
    }

    // DTO para múltiples votos
    public class EmitirMultipleRequestDto
    {
        public string Cedula { get; set; } = "";
        public int EleccionId { get; set; }
        public List<VotoIndividualDto> Votos { get; set; } = new();
    }

    public class VotoIndividualDto
    {
        public int ListaId { get; set; }
        public string CedulaCandidato { get; set; } = "";
        public string RolPostulante { get; set; } = "";
    }
}
