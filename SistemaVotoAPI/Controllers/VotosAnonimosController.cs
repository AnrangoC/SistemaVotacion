using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using SistemaVotoAPI.Data;
using SistemaVotoAPI.Models;
using SistemaVotoAPI.Services.EmailServices;
using SistemaVotoModelos;
using SistemaVotoModelos.DTOs;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace SistemaVotoAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VotosAnonimosController : ControllerBase
    {
        private readonly APIVotosDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IWebHostEnvironment _env;

        public VotosAnonimosController(APIVotosDbContext context, IEmailService emailService, IWebHostEnvironment env)
        {
            _context = context;
            _emailService = emailService;
            _env = env;
        }

        [HttpPost("validar-y-marcar")]
        public async Task<IActionResult> ValidarYMarcarVotante([FromBody] ValidacionVotoDto dto)
        {
            if (dto == null) return BadRequest(new { mensaje = "Datos inválidos." });

            var cedula = (dto.Cedula ?? "").Trim();
            if (string.IsNullOrWhiteSpace(cedula)) return BadRequest(new { mensaje = "Debe enviar cédula." });

            var votante = await _context.Votantes.FirstOrDefaultAsync(v => v.Cedula == cedula);

            if (votante == null) return NotFound(new { mensaje = "Votante no encontrado." });
            if (votante.Estado != true) return Unauthorized(new { mensaje = "Votante no habilitado." });
            if (votante.HaVotado) return BadRequest(new { mensaje = "Ya votó en este proceso." });

            votante.HaVotado = true;
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Votante marcado preventivamente." });
        }
        [HttpGet("TestEmail")]
        public async Task<IActionResult> TestEmail()
        {
            try
            {
                var emailDto = new EmailDto
                {
                    To = "cdanrangom@utn.edu.ec",
                    Subject = "Prueba desde API",
                    Body = "<b>Correo de prueba enviado desde Render</b>"
                };

                await _emailService.SendEmail(emailDto, Array.Empty<byte>(), "prueba.pdf");

                return Ok("Correo enviado correctamente.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error enviando correo: " + ex.Message);
            }
        }
        [HttpPost("EmitirVotacion")]
        public async Task<IActionResult> EmitirVotacion([FromBody] RegistroVotoDto request)
        {
            if (request == null || request.Votos == null || !request.Votos.Any())
                return BadRequest("No se recibieron datos de votación.");

            var cedula = (request.Cedula ?? "").Trim();
            if (string.IsNullOrWhiteSpace(cedula))
                return BadRequest("Debe enviar cédula.");

            if (request.EleccionId <= 0)
                return BadRequest("EleccionId inválido.");

            var votante = await _context.Votantes.FindAsync(cedula);

            if (votante == null || votante.Estado != true)
                return Unauthorized("Votante no habilitado o inexistente.");

            if (votante.HaVotado)
                return Conflict("El sistema detecta que usted ya ha participado en esta elección.");

            var eleccion = await _context.Elecciones.FindAsync(request.EleccionId);
            if (eleccion == null || eleccion.Estado != "ACTIVA")
                return BadRequest("La elección no se encuentra activa.");

            if (!votante.JuntaId.HasValue)
                return BadRequest("Error: No tiene una mesa de votación asignada.");

            var junta = await _context.Juntas
                .Include(j => j.Direccion)
                .FirstOrDefaultAsync(j => j.Id == votante.JuntaId.Value);

            if (junta == null)
                return BadRequest("Error: No tiene una mesa de votación asignada.");

            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
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
                            CedulaCandidato = (v.CedulaCandidato ?? "").Trim(),
                            RolPostulante = (v.RolPostulante ?? "").Trim()
                        };

                        _context.VotosAnonimos.Add(nuevoVoto);
                    }

                    votante.HaVotado = true;

                    await _context.SaveChangesAsync();

                    byte[]? pdfBytes = null;

                    try
                    {
                        byte[] fotoBytes;

                        using (var http = new HttpClient())
                        {
                            http.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
                            var url = (votante.FotoUrl ?? "").Trim();
                            fotoBytes = string.IsNullOrWhiteSpace(url) ? Array.Empty<byte>() : await http.GetByteArrayAsync(url);
                        }

                        var datos = new CertificadoVotacionRequest
                        {
                            Nombre = votante.NombreCompleto,
                            Cedula = votante.Cedula,
                            Provincia = junta.Direccion?.Provincia ?? "N/A",
                            Canton = junta.Direccion?.Canton ?? "N/A",
                            Parroquia = junta.Direccion?.Parroquia ?? "N/A",
                            RutaEscudo = Path.Combine(_env.WebRootPath, "imagenes", "escudo.png"),
                            RutaCne = Path.Combine(_env.WebRootPath, "imagenes", "cne.png"),
                            FotoBytes = fotoBytes,
                            RutaQr = Path.Combine(_env.WebRootPath, "imagenes", "qr.png"),
                            RutaFirma = Path.Combine(_env.WebRootPath, "imagenes", "firma.png")
                        };

                        var documento = new CertificadoDocument(datos);
                        pdfBytes = documento.GeneratePdf();
                    }
                    catch
                    {
                        pdfBytes = null;
                    }

                    if (!string.IsNullOrWhiteSpace(votante.Email))
                    {
                        try
                        {
                            var emailDto = new EmailDto
                            {
                                To = votante.Email,
                                Subject = "Certificado de Votación Oficial - Elecciones " + DateTime.Now.Year,
                                Body = $"Hola {votante.NombreCompleto}, adjuntamos tu certificado de votación."
                            };

                            await _emailService.SendEmail(emailDto, pdfBytes, $"Certificado{votante.Cedula}.pdf");
                        }
                        catch
                        {
                        }
                    }

                    await transaction.CommitAsync();

                    return Ok(new { mensaje = "Sufragio procesado exitosamente." });
                }
                catch
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, "Error interno al procesar su votación. Intente de nuevo.");
                }
            });
        }
    }
}