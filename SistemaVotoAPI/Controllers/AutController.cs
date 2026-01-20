using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaVotoAPI.Data;
using SistemaVotoAPI.Security;
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
        public async Task<IActionResult> LoginGestion(string cedula, string password)
        {
            // Se busca al usuario por cédula, verificando que sea admin o jefe de junta
            var usuario = await _context.Votantes
                .FirstOrDefaultAsync(v =>
                    v.Cedula == cedula &&
                    (v.RolId == 1 || v.RolId == 3) &&
                    v.Estado == true);

            if (usuario == null)
                return Unauthorized("Credenciales incorrectas");

            // Se compara la contraseña ingresada con el hash almacenado
            bool passwordValido = PasswordHasher.Verify(password, usuario.Password);
            if (!passwordValido)
                return Unauthorized("Credenciales incorrectas");

            // Se devuelve solo la información necesaria para el sistema
            return Ok(new
            {
                usuario.Cedula,
                usuario.NombreCompleto,
                usuario.Email,
                usuario.RolId,
                usuario.JuntaId
            });
        }

        // GENERAR TOKEN PARA VOTANTE
        // El jefe de junta valida al votante presencialmente y genera el token
        // POST: api/Aut/GenerarToken
        [HttpPost("GenerarToken")]
        public async Task<IActionResult> GenerarToken(string cedulaJefe, string cedulaVotante)
        {
            // Se valida que quien genera el token sea jefe de junta
            var jefe = await _context.Votantes.FindAsync(cedulaJefe);
            if (jefe == null || jefe.RolId != 3)
                return Unauthorized("Solo el jefe de junta puede generar tokens");

            // Se busca al votante por su cédula
            var votante = await _context.Votantes.FindAsync(cedulaVotante);
            if (votante == null)
                return NotFound("Votante no existe");

            // El jefe solo puede generar tokens para votantes de su misma junta
            if (jefe.JuntaId != votante.JuntaId)
                return Unauthorized("El votante no pertenece a su junta");

            // Solo los votantes pueden recibir token
            if (votante.RolId != 2)
                return BadRequest("Solo votantes pueden recibir token");

            // Si el votante ya sufragó, no puede recibir otro token
            if (votante.HaVotado)
                return Conflict("El votante ya sufragó");

            // Se invalidan tokens anteriores en caso de existir
            var tokensPrevios = await _context.TokensAcceso
                .Where(t => t.VotanteId == votante.Cedula && t.EsValido)
                .ToListAsync();

            foreach (var t in tokensPrevios)
                t.EsValido = false;

            // GENERACIÓN DE TOKEN NUMÉRICO (ACTUAL)
            // Token simple de seis dígitos, fácil de ingresar en votación presencial
            var token = new TokenAcceso
            {
                Codigo = new Random().Next(100000, 999999).ToString(),
                VotanteId = votante.Cedula,
                EsValido = true,
                FechaCreacion = DateTime.Now
            };

            /*
            // ALTERNATIVA COMENTADA: TOKEN ALFANUMÉRICO
            // Se deja como referencia en caso de cambiar el formato del token en el futuro
            // Más combinaciones, pero menos práctico para ingreso manual

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
        // El votante ingresa su cédula y el token generado por el jefe de junta
        // POST: api/Aut/ValidarToken
        [HttpPost("ValidarToken")]
        public async Task<IActionResult> ValidarToken(string cedula, string codigo)
        {
            // Se valida que el token exista, corresponda al votante y esté activo
            var token = await _context.TokensAcceso
                .FirstOrDefaultAsync(t =>
                    t.VotanteId == cedula &&
                    t.Codigo == codigo &&
                    t.EsValido);

            if (token == null)
                return Unauthorized("Token inválido o usado");

            // Se verifica que el votante aún no haya votado
            var votante = await _context.Votantes.FindAsync(cedula);
            if (votante == null || votante.HaVotado)
                return Unauthorized("Acceso no permitido");

            // El token se invalida inmediatamente después de ser usado
            token.EsValido = false;
            await _context.SaveChangesAsync();

            // Aquí solo se permite el acceso a la votación, el voto se registra después
            return Ok("Acceso concedido");
        }

        /*
        // Por probar 
        // Genera un token alfanumérico de longitud variable
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
