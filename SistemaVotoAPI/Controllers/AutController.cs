using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaVotoAPI.Data;
using SistemaVotoAPI.Security;
using SistemaVotoAPI.DTOs;
using SistemaVotoModelos;
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

        // LOGIN PARA ADMIN (1) Y JEFE DE JUNTA (3)
        // POST: api/Aut/LoginGestion
        [HttpPost("LoginGestion")]
        public async Task<IActionResult> LoginGestion([FromBody] LoginRequestDto request)
        {
            var usuario = await _context.Votantes
                .FirstOrDefaultAsync(v =>
                    v.Cedula == request.Cedula &&
                    (v.RolId == 1 || v.RolId == 3) &&
                    v.Estado == true);

            if (usuario == null)
                return Unauthorized("Credenciales incorrectas");

            bool passwordValido = PasswordHasher.Verify(request.Password, usuario.Password);
            if (!passwordValido)
                return Unauthorized("Credenciales incorrectas");

            return Ok(new LoginResponseDto
            {
                Cedula = usuario.Cedula,
                NombreCompleto = usuario.NombreCompleto,
                Email = usuario.Email,
                RolId = usuario.RolId,
                JuntaId = usuario.JuntaId
            });
        }



        // GENERAR TOKEN PARA VOTANTE
        // POST: api/Aut/GenerarToken
        [HttpPost("GenerarToken")]
        public async Task<IActionResult> GenerarToken(string cedulaJefe, string cedulaVotante)
        {
            var jefe = await _context.Votantes.FindAsync(cedulaJefe);
            if (jefe == null || jefe.RolId != 3)
                return Unauthorized("Solo el jefe de junta puede generar tokens");

            var votante = await _context.Votantes.FindAsync(cedulaVotante);
            if (votante == null)
                return NotFound("Votante no existe");

            if (jefe.JuntaId != votante.JuntaId)
                return Unauthorized("El votante no pertenece a su junta");

            if (votante.RolId != 2)
                return BadRequest("Solo votantes pueden recibir token");

            if (votante.HaVotado)
                return Conflict("El votante ya sufragó");

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

            /*
            // TOKEN ALFANUMÉRICO (opcional)
            var token = new TokenAcceso
            {
                Codigo = GenerarTokenAlfanumerico(6),
                VotanteId = votante.Cedula,
                EsValido = true,
                FechaCreacion = DateTime.Now
            };
            */

            _context.TokensAcceso.Add(token);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                votante.Cedula,
                token.Codigo
            });
        }

        // VALIDAR TOKEN EN LA URNA
        // POST: api/Aut/ValidarToken
        [HttpPost("ValidarToken")]
        public async Task<IActionResult> ValidarToken(string cedula, string codigo)
        {
            var token = await _context.TokensAcceso
                .FirstOrDefaultAsync(t =>
                    t.VotanteId == cedula &&
                    t.Codigo == codigo &&
                    t.EsValido);

            if (token == null)
                return Unauthorized("Token inválido o usado");

            var votante = await _context.Votantes.FindAsync(cedula);
            if (votante == null || votante.HaVotado)
                return Unauthorized("Acceso no permitido");

            token.EsValido = false;
            await _context.SaveChangesAsync();

            return Ok("Acceso concedido");
        }

        /*
        private string GenerarTokenAlfanumerico(int longitud)
        {
            const string caracteres = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(
                Enumerable.Repeat(caracteres, longitud)
                          .Select(s => s[random.Next(s.Length)])
                          .ToArray()
            );
        }
        */
    }
}
