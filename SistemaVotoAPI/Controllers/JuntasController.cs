using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaVotoAPI.Data;
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
    public class JuntasController : ControllerBase
    {
        private readonly APIVotosDbContext _context;

        public JuntasController(APIVotosDbContext context)
        {
            _context = context;
        }

        // ESTADOS: 1=Cerrada | 2=Abierta | 3=Pendiente de aprobación | 4=Aprobada

        private static bool AplicarTransicionEstado(Junta j, DateTime ahora)
        {
            if (j.Eleccion == null) return false;

            bool cambio = false;

            if (ahora < j.Eleccion.FechaInicio)
            {
                if (j.Estado != 1 && j.Estado != 4)
                {
                    j.Estado = 1;
                    cambio = true;
                }
            }
            else if (ahora >= j.Eleccion.FechaInicio && ahora < j.Eleccion.FechaFin)
            {
                if (j.Estado == 1)
                {
                    j.Estado = 2;
                    cambio = true;
                }
            }
            else
            {
                if (j.Estado == 1 || j.Estado == 2)
                {
                    j.Estado = 3;
                    cambio = true;
                }
            }

            return cambio;
        }

        private static JuntaDetalleDto Mapear(Junta j)
        {
            return new JuntaDetalleDto
            {
                Id = j.Id,
                NumeroMesa = j.NumeroMesa,
                DireccionId = j.DireccionId,
                EleccionId = j.EleccionId,
                Ubicacion = j.Direccion == null
                    ? "Sin dirección"
                    : $"{j.Direccion.Provincia} - {j.Direccion.Canton} - {j.Direccion.Parroquia}",
                NombreJefe = j.JefeDeJunta != null
                    ? j.JefeDeJunta.NombreCompleto ?? "Nombre vacío"
                    : (string.IsNullOrWhiteSpace(j.JefeDeJuntaId) ? "Sin asignar" : $"Cédula: {j.JefeDeJuntaId}"),
                EstadoJunta = j.Estado
            };
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<JuntaDetalleDto>>> GetJuntas()
        {
            var ahora = DateTime.Now;

            var juntasLista = await _context.Juntas
                .Include(j => j.Eleccion)
                .Include(j => j.Direccion)
                .Include(j => j.JefeDeJunta)
                .ToListAsync();

            bool huboCambios = false;

            foreach (var j in juntasLista)
            {
                if (AplicarTransicionEstado(j, ahora))
                    huboCambios = true;
            }

            if (huboCambios)
                await _context.SaveChangesAsync();

            var respuesta = juntasLista
                .Select(Mapear)
                .OrderBy(x => x.Id)
                .ToList();

            return Ok(respuesta);
        }

        [HttpPut("AsignarJefe")]
        public async Task<IActionResult> AsignarJefe(long juntaId, string cedulaVotante)
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

            if (juntaId > int.MaxValue)
                return BadRequest("El id de junta excede el rango permitido para el votante.");

            if (votante.JuntaId != (int)juntaId)
                return BadRequest("El votante no pertenece a esta mesa.");

            junta.JefeDeJuntaId = cedulaVotante;

            votante.RolId = 3;
            votante.JuntaId = (int)juntaId;

            await _context.SaveChangesAsync();
            return Ok("Jefe de junta asignado correctamente.");
        }

        [HttpPut("AprobarJunta/{id}")]
        public async Task<IActionResult> AprobarJunta(long id)
        {
            var junta = await _context.Juntas.FindAsync(id);
            if (junta == null)
                return NotFound();

            if (junta.Estado != 3)
                return BadRequest("Solo se pueden aprobar juntas en estado 'En espera' (3).");

            junta.Estado = 4;
            await _context.SaveChangesAsync();
            return Ok("Junta aprobada. Resultados cargados.");
        }

        [HttpPost("CrearPorDireccion")]
        public async Task<IActionResult> CrearPorDireccion(int eleccionId, int direccionId, int cantidad)
        {
            var eleccionExiste = await _context.Elecciones.AnyAsync(e => e.Id == eleccionId);
            if (!eleccionExiste)
                return BadRequest("La elección no existe.");

            int existentes = await _context.Juntas.CountAsync(j => j.EleccionId == eleccionId && j.DireccionId == direccionId);

            var nuevasJuntas = new List<Junta>();

            for (int i = 1; i <= cantidad; i++)
            {
                int num = existentes + i;

                nuevasJuntas.Add(new Junta
                {
                    Id = long.Parse($"{eleccionId}{direccionId:D6}{num:D2}"),
                    NumeroMesa = num,
                    DireccionId = direccionId,
                    EleccionId = eleccionId,
                    Estado = 1
                });
            }

            _context.Juntas.AddRange(nuevasJuntas);
            await _context.SaveChangesAsync();

            return Ok("Juntas creadas.");
        }

        [HttpGet("DeJefeActual/{cedula}")]
        public async Task<IActionResult> GetJuntaDeJefeActual(string cedula)
        {
            cedula = (cedula ?? "").Trim();
            if (string.IsNullOrWhiteSpace(cedula))
                return BadRequest("Cédula inválida.");

            var ahora = DateTime.Now;

            var eleccionActiva = await _context.Elecciones
                .Where(e => e.FechaInicio <= ahora && e.FechaFin >= ahora)
                .OrderByDescending(e => e.FechaInicio)
                .FirstOrDefaultAsync();

            if (eleccionActiva == null)
                return NotFound("No hay elección activa.");

            var votante = await _context.Votantes.FindAsync(cedula);
            if (votante == null)
                return NotFound("Votante no existe.");

            if (votante.RolId != 3)
                return Conflict("No eres jefe de junta.");

            if (!votante.JuntaId.HasValue)
                return NotFound("No tienes junta asignada.");

            var junta = await _context.Juntas
                .FirstOrDefaultAsync(j => j.Id == votante.JuntaId.Value && j.EleccionId == eleccionActiva.Id);

            if (junta == null)
                return NotFound("Tu junta asignada pertenece a otra elección.");

            return Ok(junta);
        }

        [HttpGet("PorEleccion/{eleccionId:int}")]
        public async Task<ActionResult<IEnumerable<JuntaDetalleDto>>> GetJuntasPorEleccion(int eleccionId)
        {
            var ahora = DateTime.Now;

            var juntasLista = await _context.Juntas
                .Where(j => j.EleccionId == eleccionId)
                .Include(j => j.Eleccion)
                .Include(j => j.Direccion)
                .Include(j => j.JefeDeJunta)
                .ToListAsync();

            bool huboCambios = false;

            foreach (var j in juntasLista)
            {
                if (AplicarTransicionEstado(j, ahora))
                    huboCambios = true;
            }

            if (huboCambios)
                await _context.SaveChangesAsync();

            var respuesta = juntasLista
                .Select(Mapear)
                .OrderBy(x => x.NumeroMesa)
                .ToList();

            return Ok(respuesta);
        }
    }
}
