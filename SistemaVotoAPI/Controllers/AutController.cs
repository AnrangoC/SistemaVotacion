using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaVotoAPI.Data;
using SistemaVotoAPI.DTOs;
using SistemaVotoAPI.Security; // Donde se encuentra tu PasswordHasher
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

        // POST: api/Aut/LoginGestion
        [HttpPost("LoginGestion")]
        public async Task<IActionResult> LoginGestion([FromBody] LoginRequestDto request)
        {
            if (request == null || string.IsNullOrEmpty(request.Cedula) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest("Datos de inicio de sesión incompletos.");
            }

            // 1. Buscar al usuario por cédula. 
            // Filtramos para que solo entren Roles 1 (Admin) o 3 (Jefe de Junta) y que estén activos (Estado == true)
            var usuario = await _context.Votantes
                .FirstOrDefaultAsync(v => v.Cedula == request.Cedula && v.Estado == true);

            if (usuario == null)
            {
                return Unauthorized("Usuario no encontrado o inactivo.");
            }

            // 2. Verificar si tiene el Rol permitido para la gestión
            if (usuario.RolId != 1 && usuario.RolId != 3)
            {
                return Unauthorized("El usuario no tiene permisos de gestión.");
            }

            // 3. Verificar la contraseña usando tu clase de seguridad
            bool isPasswordValid = PasswordHasher.Verify(request.Password, usuario.Password);

            if (!isPasswordValid)
            {
                return Unauthorized("Cédula o contraseña incorrecta.");
            }

            // 4. Responder con los datos necesarios para que el MVC cree la sesión/cookie
            var response = new LoginResponseDto
            {
                Cedula = usuario.Cedula,
                NombreCompleto = usuario.NombreCompleto,
                Email = usuario.Email,
                RolId = usuario.RolId,
                JuntaId = usuario.JuntaId
            };

            return Ok(response);
        }

        // Si necesitas un método para votantes normales (Opcional)
        [HttpPost("LoginVotante")]
        public async Task<IActionResult> LoginVotante([FromBody] LoginRequestDto request)
        {
            var usuario = await _context.Votantes
                .FirstOrDefaultAsync(v => v.Cedula == request.Cedula && v.RolId == 2 && v.Estado == true);

            if (usuario == null || !PasswordHasher.Verify(request.Password, usuario.Password))
            {
                return Unauthorized("Credenciales inválidas para votante.");
            }

            return Ok(new { usuario.Cedula, usuario.NombreCompleto, usuario.RolId });
        }
    }
}