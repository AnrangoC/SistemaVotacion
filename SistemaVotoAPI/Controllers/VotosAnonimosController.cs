using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaVotoAPI.Data;
using SistemaVotoAPI.Services.EmailServices;
using SistemaVotoAPI.Models;
using SistemaVotoModelos;
using SistemaVotoModelos.DTOs;
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
        private readonly IEmailService _emailService; // Inyectamos la interfaz

        public VotosAnonimosController(APIVotosDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

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

        [HttpPost("EmitirVotacion")]
        public async Task<IActionResult> EmitirVotacion([FromBody] RegistroVotoDto request)
        {
            if (request == null || request.Votos == null || !request.Votos.Any())
                return BadRequest("No se recibieron datos de votación.");

            var votante = await _context.Votantes.FindAsync(request.Cedula.Trim());
            if (votante == null || votante.Estado != true)
                return Unauthorized("Votante no habilitado o inexistente.");

            if (votante.HaVotado)
                return Conflict("El sistema detecta que usted ya ha participado en esta elección.");

            var eleccion = await _context.Elecciones.FindAsync(request.EleccionId);
            if (eleccion == null || eleccion.Estado != "ACTIVA")
                return BadRequest("La elección no se encuentra activa.");

            var junta = await _context.Juntas.FindAsync(votante.JuntaId);
            if (junta == null)
                return BadRequest("Error: No tiene una mesa de votación asignada.");

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

                    votante.HaVotado = true;
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    // --- ENVÍO DE EMAIL TRAS VOTO EXITOSO ---
                    try
                    {
                        var emailRequest = new EmailDto
                        {
                            To = votante.Email, // Asegúrate que el campo se llame 'Correo' o 'Email' en tu modelo
                            Subject = "Certificado de Sufragio - Sistema de Votación",
                            Body = $"<h1>¡Gracias por votar!</h1><p>Estimado {votante.NombreCompleto}, adjuntamos su certificado de votación electrónica.</p>"
                        };

                        // Aquí deberías tener la lógica que genera el PDF en bytes. 
                        // Por ahora pasamos un array vacío o el archivo generado.
                        byte[] archivoPdf = new byte[0];

                        _emailService.SendEmail(emailRequest, archivoPdf, $"Certificado_{votante.Cedula}.pdf");
                    }
                    catch (Exception)
                    {
                        // No lanzamos error para no afectar la experiencia del usuario si solo falla el mail
                    }

                    return Ok(new { mensaje = "Sufragio procesado exitosamente." });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, "Error interno al procesar su votación.");
                }
            });
        }
    }
}