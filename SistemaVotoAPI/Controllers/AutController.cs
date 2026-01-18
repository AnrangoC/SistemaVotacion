using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaVotoAPI.Data;
using SistemaVotoModelos;

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
        public async Task<IActionResult> LoginGestion(string cedula, string password)
        {
            var usuario = await _context.Votantes
                .FirstOrDefaultAsync(v =>
                    v.Cedula == cedula &&
                    v.Password == password &&
                    (v.RolId == 1 || v.RolId == 3));

            if (usuario == null)
                return Unauthorized("Credenciales incorrectas");

            return Ok(usuario);
        }
        // GENERAR TOKEN PARA VOTANTE (ROL 2)
        // POST: api/Aut/GenerarToken
        [HttpPost("GenerarToken")]
        public async Task<IActionResult> GenerarToken(string cedulaVotante)
        {
            var votante = await _context.Votantes.FindAsync(cedulaVotante);

            if (votante == null)
                return NotFound("Votante no existe");

            if (votante.RolId != 2)
                return BadRequest("Solo votantes pueden recibir token");

            if (votante.HaVotado)
                return Conflict("El votante ya sufragó");

            var token = new TokenAcceso
            {
                Codigo = new Random().Next(100000, 999999).ToString(),
                VotanteId = votante.Cedula,
                EsValido = true,
                FechaCreacion = DateTime.Now
            };

            _context.TokensAcceso.Add(token);
            await _context.SaveChangesAsync();

            return Ok(token);
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

            token.EsValido = false;
            await _context.SaveChangesAsync();

            return Ok("Acceso concedido");
        }
    }
}
