using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaVotoAPI.Data;
using SistemaVotoModelos;
using SistemaVotoModelos.DTOs; // Asegúrate de que los DTOs estén aquí
using System;
using System.Collections.Generic;
using System.Linq;
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

        // Método para bloquear al votante apenas entra a la papeleta (opcional/seguridad)
        [HttpPost("validar-y-marcar")]
        public async Task<IActionResult> ValidarYMarcarVotante([FromBody] ValidacionVotoDto dto)
        {
            var votante = await _context.Votantes.FirstOrDefaultAsync(v => v.Cedula == dto.Cedula);

            if (votante == null) return NotFound(new { mensaje = "Votante no encontrado." });
            if (votante.HaVotado) return BadRequest(new { mensaje = "Ya votó en este proceso." });

            votante.HaVotado = true;
            await _context.SaveChangesAsync();
            return Ok(new { mensaje = "Votante marcado preventivamente." });
        }

        // MÉTODO PRINCIPAL: Registro de N votos bajo una sola transacción
        [HttpPost("EmitirVotacion")]
        public async Task<IActionResult> EmitirVotacion([FromBody] RegistroVotoDto request)
        {
            if (request == null || request.Votos == null || !request.Votos.Any())
                return BadRequest("No se recibieron datos de votación.");

            // 1. Validación del Votante
            var votante = await _context.Votantes.FindAsync(request.Cedula.Trim());

            if (votante == null || votante.Estado != true)
                return Unauthorized("Votante no habilitado o inexistente.");

            if (votante.HaVotado)
                return Conflict("El sistema detecta que usted ya ha participado en esta elección.");

            // 2. Validación de Elección y Mesa
            var eleccion = await _context.Elecciones.FindAsync(request.EleccionId);
            if (eleccion == null || eleccion.Estado != "ACTIVA")
                return BadRequest("La elección no se encuentra activa.");

            var junta = await _context.Juntas.FindAsync(votante.JuntaId);
            if (junta == null)
                return BadRequest("Error: No tiene una mesa de votación asignada.");

            // 3. PROCESO TRANSACCIONAL (Atómico)
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    foreach (var v in request.Votos)
                    {
                        var nuevoVoto = new VotoAnonimo
                        {
                            FechaVoto = DateTime.Now,
                            EleccionId = request.EleccionId,
                            DireccionId = junta.DireccionId,
                            NumeroMesa = junta.NumeroMesa,
                            ListaId = v.ListaId,
                            CedulaCandidato = v.CedulaCandidato?.Trim(),
                            RolPostulante = v.RolPostulante?.Trim()
                        };
                        _context.VotosAnonimos.Add(nuevoVoto);
                    }

                    // Marcamos que ya participó (esto es lo que se limpia en cada nueva elección activa)
                    votante.HaVotado = true;

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Ok(new { mensaje = "Sufragio procesado exitosamente." });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    // Log del error ex si fuera necesario
                    return StatusCode(500, "Error interno al procesar su votación. Intente de nuevo.");
                }
            });
        }
    }
}