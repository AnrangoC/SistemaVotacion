using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaVotoAPI.Data;
using SistemaVotoModelos;

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

        // GET: api/Juntas
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Junta>>> GetJuntas()
        {
            return await _context.Juntas.ToListAsync();
        }

        // GET: api/Juntas/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Junta>> GetJunta(int id)
        {
            var junta = await _context.Juntas.FindAsync(id);

            if (junta == null)
            {
                return NotFound();
            }

            return junta;
        }

        // PUT: api/Juntas/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutJunta(int id, Junta junta)
        {
            if (id != junta.Id)
            {
                return BadRequest();
            }

            _context.Entry(junta).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!JuntaExists(id))
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

        // POST: api/Juntas
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Junta>> PostJunta(Junta junta)
        {
            _context.Juntas.Add(junta);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetJunta", new { id = junta.Id }, junta);
        }

        // DELETE: api/Juntas/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteJunta(int id)
        {
            var junta = await _context.Juntas.FindAsync(id);
            if (junta == null)
            {
                return NotFound();
            }

            _context.Juntas.Remove(junta);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool JuntaExists(int id)
        {
            return _context.Juntas.Any(e => e.Id == id);
        }
    }
}
