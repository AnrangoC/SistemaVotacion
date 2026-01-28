using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaVotoAPI.Data;
using SistemaVotoModelos;
using SistemaVotoModelos.DTOs;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SistemaVotoAPI.Controllers
{
    // Nota: por ahora la seguridad la controla el MVC con cookies y rol 1
    // Luego podemos migrar esto a JWT y volver a activar [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class JuntasController : ControllerBase
    {
        private readonly APIVotosDbContext _context;

        public JuntasController(APIVotosDbContext context)
        {
            _context = context;
        }

        // Estados Junta (int)
        // 1=Cerrada | 2=Abierta | 3=Pendiente de aprobación | 4=Aprobada

        [HttpGet]
        public async Task<ActionResult<IEnumerable<JuntaDetalleDto>>> GetJuntas()
        {
            var juntas = await _context.Juntas
                .Include(j => j.Direccion)
                .Include(j => j.JefeDeJunta)
                .Select(j => new JuntaDetalleDto
                {
                    Id = j.Id,
                    NumeroMesa = j.NumeroMesa,
                    Ubicacion = $"{j.Direccion.Provincia} - {j.Direccion.Canton} - {j.Direccion.Parroquia}",
                    NombreJefe = string.IsNullOrWhiteSpace(j.JefeDeJuntaId)
                        ? "Sin asignar"
                        : (j.JefeDeJunta != null ? j.JefeDeJunta.NombreCompleto : "Sin asignar"),
                    EstadoJunta = j.Estado
                })
                .OrderBy(j => j.Id)
                .ToListAsync();

            return Ok(juntas);
        }


        [HttpPost("CrearPorDireccion")]
        public async Task<IActionResult> CrearPorDireccion(int eleccionId, int direccionId, int cantidad)
        {
            if (eleccionId <= 0)
                return BadRequest("Elección inválida.");

            var eleccionExiste = await _context.Elecciones.AnyAsync(e => e.Id == eleccionId);
            if (!eleccionExiste)
                return BadRequest("La elección no existe.");

            var direccion = await _context.Direcciones.FindAsync(direccionId);
            if (direccion == null)
                return BadRequest("La dirección no existe.");

            if (cantidad <= 0)
                return BadRequest("La cantidad debe ser mayor a cero.");

            int mesasExistentes = await _context.Juntas
                .CountAsync(j => j.EleccionId == eleccionId && j.DireccionId == direccionId);

            var nuevasJuntas = new List<Junta>();

            for (int i = 1; i <= cantidad; i++)
            {
                int numeroMesa = mesasExistentes + i;

                int juntaId = int.Parse($"{direccion.Id}{numeroMesa:D2}");

                nuevasJuntas.Add(new Junta
                {
                    Id = juntaId,
                    NumeroMesa = numeroMesa,
                    DireccionId = direccion.Id,
                    EleccionId = eleccionId,
                    JefeDeJuntaId = null,
                    Estado = 1
                });
            }

            _context.Juntas.AddRange(nuevasJuntas);
            await _context.SaveChangesAsync();

            return Ok("Juntas creadas correctamente.");
        }


        [HttpPut("AsignarJefe")]
        public async Task<IActionResult> AsignarJefe(int juntaId, string cedulaVotante)
        {
            if (string.IsNullOrWhiteSpace(cedulaVotante))
                return BadRequest("Cédula obligatoria.");

            cedulaVotante = cedulaVotante.Trim();

            var junta = await _context.Juntas.FindAsync(juntaId);
            if (junta == null)
                return NotFound("Junta no encontrada");

            var votante = await _context.Votantes.FindAsync(cedulaVotante);
            if (votante == null)
                return NotFound("El votante no existe");

            if (!votante.JuntaId.HasValue || votante.JuntaId.Value != juntaId)
                return BadRequest("Ese votante no pertenece a esta junta.");

            if (votante.RolId == 1)
                return BadRequest("Un administrador no puede ser jefe de junta.");

            bool esCandidato = await _context.Candidatos.AnyAsync(c => c.Cedula == cedulaVotante);
            if (esCandidato)
                return BadRequest("Un candidato no puede ser jefe de junta.");

            junta.JefeDeJuntaId = cedulaVotante;
            votante.RolId = 3;

            await _context.SaveChangesAsync();
            return Ok("Jefe de junta asignado correctamente");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteJunta(int id)
        {
            var junta = await _context.Juntas.FindAsync(id);
            if (junta == null)
                return NotFound();

            _context.Juntas.Remove(junta);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
