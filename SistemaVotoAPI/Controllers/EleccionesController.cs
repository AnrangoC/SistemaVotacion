using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaVotoAPI.Data;
using SistemaVotoModelos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SistemaVotoAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EleccionesController : ControllerBase
    {
        private readonly APIVotosDbContext _context;

        public EleccionesController(APIVotosDbContext context)
        {
            _context = context;
        }

        private static string CalcularEstado(Eleccion e, DateTime now)
        {
            if (now < e.FechaInicio) return "CONFIGURACION";
            if (now >= e.FechaInicio && now < e.FechaFin) return "ACTIVA";
            return "FINALIZADA";
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Eleccion>>> GetEleccion()
        {
            var now = DateTime.Now;
            
            var elecciones = await _context.Elecciones.ToListAsync();

            bool huboCambios = false;
            foreach (var e in elecciones)
            {
                var nuevoEstado = CalcularEstado(e, now);
                if (e.Estado != nuevoEstado)
                {
                    e.Estado = nuevoEstado;
                    huboCambios = true;
                }
            }

            if (huboCambios)
                await _context.SaveChangesAsync();

            return elecciones;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Eleccion>> GetEleccion(int id)
        {
            var eleccion = await _context.Elecciones.FindAsync(id);
            if (eleccion == null) return NotFound();

            var now = DateTime.Now;
            var nuevoEstado = CalcularEstado(eleccion, now);

            if (eleccion.Estado != nuevoEstado)
            {
                eleccion.Estado = nuevoEstado;
                await _context.SaveChangesAsync();
            }

            return eleccion;
        }

        [HttpPost]
        public async Task<ActionResult<Eleccion>> PostEleccion(Eleccion eleccion)
        {
            if (eleccion == null) return BadRequest("Datos no proporcionados.");

            eleccion.Titulo = (eleccion.Titulo ?? "").Trim();
            if (string.IsNullOrWhiteSpace(eleccion.Titulo))
                return BadRequest("El título es obligatorio.");

            if (eleccion.FechaFin <= eleccion.FechaInicio)
                return BadRequest("La fecha/hora fin debe ser mayor que la fecha/hora inicio.");

            eleccion.Estado = "CONFIGURACION";

            _context.Elecciones.Add(eleccion);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetEleccion), new { id = eleccion.Id }, eleccion);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutEleccion(int id, Eleccion eleccion)
        {
            if (eleccion == null) return BadRequest("Datos no proporcionados.");
            if (id != eleccion.Id) return BadRequest("Id no coincide.");

            var existente = await _context.Elecciones.FindAsync(id);
            if (existente == null) return NotFound();

            // Si la fecha actual es igual o mayor a la fecha de inicio guardada, se bloquea la edición.
            if (DateTime.Now >= existente.FechaInicio)
            {
                return BadRequest("No se puede editar una elección que ya ha comenzado o que ya finalizó.");
            }

            var titulo = (eleccion.Titulo ?? "").Trim();
            if (string.IsNullOrWhiteSpace(titulo))
                return BadRequest("El título es obligatorio.");

            if (eleccion.FechaFin <= eleccion.FechaInicio)
                return BadRequest("La fecha/hora fin debe ser mayor que la fecha/hora inicio.");

            existente.Titulo = titulo;
            existente.FechaInicio = eleccion.FechaInicio;
            existente.FechaFin = eleccion.FechaFin;
            existente.Estado = CalcularEstado(existente, DateTime.Now);

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEleccion(int id)
        {
            var eleccion = await _context.Elecciones.FindAsync(id);
            if (eleccion == null) return NotFound();

            _context.Elecciones.Remove(eleccion);

            try
            {
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch
            {
                return Conflict("No se puede eliminar una elección con datos asociados. Usa /Forzar si deseas borrar todo.");
            }
        }

        [HttpDelete("{id}/Forzar")]
        public async Task<IActionResult> DeleteEleccionForzar(int id)
        {
            var eleccion = await _context.Elecciones.FindAsync(id);
            if (eleccion == null) return NotFound("Elección no encontrada en la base de datos.");

            await using var tx = await _context.Database.BeginTransactionAsync();

            try
            {
                var juntas = await _context.Juntas
                    .Where(j => j.EleccionId == id)
                    .ToListAsync();

                var juntaIds = juntas.Select(j => j.Id).ToList(); // Cambiado de (int)j.Id a j.Id


                var votos = await _context.VotosAnonimos
                    .Where(v => v.EleccionId == id)
                    .ToListAsync();

                var votantesDeEsaEleccion = await _context.Votantes
                    .Where(v => v.JuntaId.HasValue && juntaIds.Contains(v.JuntaId.Value))
                    .ToListAsync();

                foreach (var v in votantesDeEsaEleccion)
                    v.HaVotado = false;

                _context.VotosAnonimos.RemoveRange(votos);

                var candidatos = await _context.Candidatos
                    .Where(c => c.EleccionId == id)
                    .ToListAsync();
                _context.Candidatos.RemoveRange(candidatos);

                var listas = await _context.Listas
                    .Where(l => l.EleccionId == id)
                    .ToListAsync();
                _context.Listas.RemoveRange(listas);

                _context.Juntas.RemoveRange(juntas);
                _context.Elecciones.Remove(eleccion);

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                return Ok("Elección eliminada junto con sus votos, listas, candidatos y juntas asociadas.");
            }
            catch
            {
                await tx.RollbackAsync();
                return StatusCode(500, "No se pudo realizar el borrado forzado. Revisa las relaciones en la base de datos.");
            }
        }

        [HttpGet("Activa")]
        public async Task<IActionResult> GetEleccionActiva()
        {
            var ahora = DateTime.Now;

            var eleccion = await _context.Elecciones
                .Where(e => e.FechaInicio <= ahora && e.FechaFin >= ahora)
                .OrderByDescending(e => e.FechaInicio)
                .FirstOrDefaultAsync();

            if (eleccion == null)
                return NotFound("No hay una elección activa en este momento.");

            return Ok(eleccion);
        }
    }
}
