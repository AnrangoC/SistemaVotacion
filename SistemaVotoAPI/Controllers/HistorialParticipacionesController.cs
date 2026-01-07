using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaVotoModelos;

namespace SistemaVotoAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HistorialParticipacionesController : ControllerBase
    {
        private readonly APIVotosDbContext _context;

        public HistorialParticipacionesController(APIVotosDbContext context)
        {
            _context = context;
        }

        // GET: api/HistorialParticipaciones
        [HttpGet]
        public async Task<ActionResult<IEnumerable<HistorialParticipacion>>> GetHistorialParticipacion()
        {
            return await _context.HistorialParticipaciones.ToListAsync();
        }

        // GET: api/HistorialParticipaciones/5
        [HttpGet("{id}")]
        public async Task<ActionResult<HistorialParticipacion>> GetHistorialParticipacion(int id)
        {
            var historialParticipacion = await _context.HistorialParticipaciones.FindAsync(id);

            if (historialParticipacion == null)
            {
                return NotFound();
            }

            return historialParticipacion;
        }

        // PUT: api/HistorialParticipaciones/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutHistorialParticipacion(int id, HistorialParticipacion historialParticipacion)
        {
            if (id != historialParticipacion.Id)
            {
                return BadRequest();
            }

            _context.Entry(historialParticipacion).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!HistorialParticipacionExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/HistorialParticipaciones
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<HistorialParticipacion>> PostHistorialParticipacion(HistorialParticipacion historialParticipacion)
        {
            _context.HistorialParticipaciones.Add(historialParticipacion);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetHistorialParticipacion", new { id = historialParticipacion.Id }, historialParticipacion);
        }

        // DELETE: api/HistorialParticipaciones/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteHistorialParticipacion(int id)
        {
            var historialParticipacion = await _context.HistorialParticipaciones.FindAsync(id);
            if (historialParticipacion == null)
            {
                return NotFound();
            }

            _context.HistorialParticipaciones.Remove(historialParticipacion);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool HistorialParticipacionExists(int id)
        {
            return _context.HistorialParticipaciones.Any(e => e.Id == id);
        }
    }
}
