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
    [Route("api/[controller]")]
    [ApiController]
    public class JuntasController : ControllerBase
    {
        private readonly APIVotosDbContext _context;

        public JuntasController(APIVotosDbContext context)
        {
            _context = context;
        }

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
                    DireccionId = j.DireccionId,
                    EleccionId = j.EleccionId,
                    Ubicacion = j.Direccion == null
                        ? "Sin dirección"
                        : $"{j.Direccion.Provincia} - {j.Direccion.Canton} - {j.Direccion.Parroquia}",
                    NombreJefe = string.IsNullOrWhiteSpace(j.JefeDeJuntaId)
                        ? "Sin asignar"
                        : (j.JefeDeJunta != null ? j.JefeDeJunta.NombreCompleto : "Sin asignar"),
                    EstadoJunta = j.Estado
                })
                .OrderBy(j => j.Id)
                .ToListAsync();

            return Ok(juntas);
        }

        // Id Junta (Opción B): EleccionId + DireccionId + Mesa
        // Ej: Eleccion 5, Direccion 010101, Mesa 01 -> 501010101
        private static long ConstruirJuntaId(int eleccionId, int direccionId, int numeroMesa)
        {
            var idStr = $"{eleccionId}{direccionId:D6}{numeroMesa:D2}";
            return long.Parse(idStr);
        }

        [HttpPost("CrearPorDireccion")]
        public async Task<IActionResult> CrearPorDireccion(int eleccionId, int direccionId, int cantidad)
        {
            if (eleccionId <= 0) return BadRequest("Elección inválida.");
            if (direccionId <= 0) return BadRequest("Dirección inválida.");
            if (cantidad <= 0) return BadRequest("La cantidad debe ser mayor a cero.");

            var eleccionExiste = await _context.Elecciones.AnyAsync(e => e.Id == eleccionId);
            if (!eleccionExiste) return BadRequest("La elección no existe.");

            var direccion = await _context.Direcciones.FindAsync(direccionId);
            if (direccion == null) return BadRequest("La dirección no existe.");

            int mesasExistentes = await _context.Juntas
                .CountAsync(j => j.EleccionId == eleccionId && j.DireccionId == direccionId);

            var nuevasJuntas = new List<Junta>();

            for (int i = 1; i <= cantidad; i++)
            {
                int numeroMesa = mesasExistentes + i;

                long juntaId = ConstruirJuntaId(eleccionId, direccion.Id, numeroMesa);

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
        public async Task<IActionResult> AsignarJefe(long juntaId, string cedulaVotante)
        {
            if (string.IsNullOrWhiteSpace(cedulaVotante))
                return BadRequest("Cédula obligatoria.");

            cedulaVotante = cedulaVotante.Trim();

            var junta = await _context.Juntas.FindAsync(juntaId);
            if (junta == null) return NotFound("Junta no encontrada");

            var votante = await _context.Votantes.FindAsync(cedulaVotante);
            if (votante == null) return NotFound("El votante no existe");

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
        public async Task<IActionResult> DeleteJunta(long id)
        {
            var junta = await _context.Juntas.FindAsync(id);
            if (junta == null) return NotFound();

            _context.Juntas.Remove(junta);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
