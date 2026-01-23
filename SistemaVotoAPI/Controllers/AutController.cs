using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaVotoAPI.Data;
using SistemaVotoAPI.Security;
using SistemaVotoModelos.DTOs; // CAMBIO: Usando el namespace centralizado
using System;
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

        [HttpPost("LoginGestion")]
        public async Task<IActionResult> LoginGestion([FromBody] LoginRequestDto request)
        {
            if (request == null || string.IsNullOrEmpty(request.Cedula) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest("Datos de inicio de sesión incompletos.");
            }

            try
            {
                // Tu lógica original
                var usuario = await _context.Votantes
                    .FirstOrDefaultAsync(v => v.Cedula == request.Cedula && v.Estado == true);

                if (usuario == null)
                {
                    return Unauthorized("Usuario no encontrado o inactivo.");
                }

                if (usuario.RolId != 1 && usuario.RolId != 3)
                {
                    return Unauthorized("El usuario no tiene permisos de gestión.");
                }

                bool isPasswordValid = PasswordHasher.Verify(request.Password, usuario.Password);
                if (!isPasswordValid)
                {
                    return Unauthorized("Cédula o contraseña incorrecta.");
                }

                // Tu mapeo original (Asegurado contra nulos)
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
            catch (Exception ex)
            {
                // Esto evita que la API se caiga y te dice qué pasó
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }
    }
}