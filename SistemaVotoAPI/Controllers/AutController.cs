using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaVotoAPI.Data;
using SistemaVotoAPI.Security;
using SistemaVotoModelos;
using SistemaVotoModelos.DTOs;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SistemaVotoAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AutController : ControllerBase
    {
        private readonly APIVotosDbContext _context;

        public AutController(APIVotosDbContext context)
        {
            _context = context;
        }

        private const int TOKEN_MINUTOS_VALIDEZ = 5;

        // Login para Admin (1) y Jefe de Junta (3)
        [HttpPost("LoginGestion")]
        public async Task<IActionResult> LoginGestion([FromBody] LoginRequestDto request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Cedula) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("Datos de inicio de sesión incompletos.");

            var cedula = request.Cedula.Trim();

            var usuario = await _context.Votantes
                .FirstOrDefaultAsync(v => v.Cedula == cedula && v.Estado == true);

            if (usuario == null)
                return Unauthorized("Usuario no encontrado o inactivo.");

            if (usuario.RolId != 1 && usuario.RolId != 3)
                return Unauthorized("El usuario no tiene permisos de gestión.");

            bool passOk = PasswordHasher.Verify(request.Password, usuario.Password);
            if (!passOk)
                return Unauthorized("Cédula o contraseña incorrecta.");

            var response = new LoginResponseDto
            {
                Cedula = usuario.Cedula,
                NombreCompleto = usuario.NombreCompleto ?? "Sin nombre",
                Email = usuario.Email ?? "Sin email",
                FotoUrl = usuario.FotoUrl,
                RolId = usuario.RolId,
                JuntaId = usuario.JuntaId
            };

            return Ok(response);
        }

        // DTO interno SOLO para soportar envío por body si algún día lo usas desde Swagger/Postman
        public class GenerarTokenRequest
        {
            public string CedulaJefe { get; set; } = string.Empty;
            public string CedulaVotante { get; set; } = string.Empty;
        }

        // Generar token: soporta QUERY o BODY
        // QUERY:  POST api/Aut/GenerarToken?cedulaJefe=...&cedulaVotante=...
        // BODY:   { "cedulaJefe": "...", "cedulaVotante": "..." }
        [HttpPost("GenerarToken")]
        public async Task<IActionResult> GenerarToken(
            [FromQuery] string? cedulaJefe,
            [FromQuery] string? cedulaVotante,
            [FromBody] GenerarTokenRequest? body
        )
        {
            // Si no vinieron por query, intento por body
            var cj = (cedulaJefe ?? body?.CedulaJefe ?? "").Trim();
            var cv = (cedulaVotante ?? body?.CedulaVotante ?? "").Trim();

            if (string.IsNullOrWhiteSpace(cj) || string.IsNullOrWhiteSpace(cv))
                return BadRequest("Debe enviar cedulaJefe y cedulaVotante.");

            var jefe = await _context.Votantes.FindAsync(cj);
            if (jefe == null || jefe.Estado != true)
                return Unauthorized("Jefe de junta no encontrado o inactivo.");

            if (jefe.RolId != 3)
                return Unauthorized("Solo el jefe de junta puede generar tokens.");

            var votante = await _context.Votantes.FindAsync(cv);
            if (votante == null)
                return NotFound("Votante no existe.");

            if (votante.Estado != true)
                return BadRequest("El votante está inactivo.");

            if (jefe.JuntaId == null || votante.JuntaId == null || jefe.JuntaId != votante.JuntaId)
                return BadRequest("El votante no pertenece a su junta.");

            //if (votante.RolId != 2)
              //  return BadRequest("Solo votantes pueden recibir token.");

            if (votante.HaVotado)
                return Conflict("El votante ya votó.");

            // Invalida tokens anteriores
            var tokensPrevios = await _context.TokensAcceso
                .Where(t => t.VotanteId == votante.Cedula && t.EsValido)
                .ToListAsync();

            foreach (var t in tokensPrevios)
                t.EsValido = false;

            var token = new TokenAcceso
            {
                Codigo = new Random().Next(100000, 999999).ToString(),
                VotanteId = votante.Cedula,
                EsValido = true,
                FechaCreacion = DateTime.Now
            };

            _context.TokensAcceso.Add(token);
            await _context.SaveChangesAsync();

            // Respuesta simple (tu MVC ya la muestra en TempData)
            return Ok(new
            {
                Cedula = votante.Cedula,
                Token = token.Codigo,
                Expira = token.FechaCreacion.AddMinutes(TOKEN_MINUTOS_VALIDEZ)
            });
        }

        // Validar token en la urna
        // Body: { "cedula": "...", "codigo": "..." }
        [HttpPost("ValidarToken")]
        public async Task<IActionResult> ValidarToken([FromBody] dynamic body)
        {
            if (body == null)
                return BadRequest("Datos inválidos.");

            string cedula = ((string)body?.cedula ?? "").Trim();
            string codigo = ((string)body?.codigo ?? "").Trim();

            if (string.IsNullOrWhiteSpace(cedula) || string.IsNullOrWhiteSpace(codigo))
                return BadRequest("Debe enviar cedula y codigo.");

            var token = await _context.TokensAcceso
                .FirstOrDefaultAsync(t => t.VotanteId == cedula && t.Codigo == codigo && t.EsValido);

            if (token == null)
                return Unauthorized("Token inválido o usado.");

            var expira = token.FechaCreacion.AddMinutes(TOKEN_MINUTOS_VALIDEZ);
            if (DateTime.Now > expira)
            {
                token.EsValido = false;
                await _context.SaveChangesAsync();
                return Unauthorized("Token expirado.");
            }

            var votante = await _context.Votantes.FindAsync(cedula);
            if (votante == null || votante.Estado != true || votante.HaVotado)
                return Unauthorized("Acceso no permitido.");

            token.EsValido = false;
            await _context.SaveChangesAsync();

            return Ok("Acceso concedido.");
        }
    }
}
