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
    public class VotosAnonimosController : ControllerBase
    {
        private readonly APIVotosDbContext _context;

        public VotosAnonimosController(APIVotosDbContext context)
        {
            _context = context;
        }

        // GET: api/VotosAnonimos
        [HttpGet]
        public async Task<ActionResult<IEnumerable<VotoAnonimo>>> GetVotoAnonimo()
        {
            return await _context.VotosAnonimos.ToListAsync();
        }

        // GET: api/VotosAnonimos/5
        [HttpGet("{id}")]
        public async Task<ActionResult<VotoAnonimo>> GetVotoAnonimo(int id)
        {
            var votoAnonimo = await _context.VotosAnonimos.FindAsync(id);

            if (votoAnonimo == null)
            {
                return NotFound();
            }

            return votoAnonimo;
        }

        // PUT: api/VotosAnonimos/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutVotoAnonimo(int id, VotoAnonimo votoAnonimo)
        {
            if (id != votoAnonimo.Id)
            {
                return BadRequest();
            }

            _context.Entry(votoAnonimo).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!VotoAnonimoExists(id))
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

        // POST: api/VotosAnonimos
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<VotoAnonimo>> PostVotoAnonimo(VotoAnonimo votoAnonimo)
        {
            _context.VotosAnonimos.Add(votoAnonimo);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetVotoAnonimo", new { id = votoAnonimo.Id }, votoAnonimo);
        }

        // DELETE: api/VotosAnonimos/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteVotoAnonimo(int id)
        {
            var votoAnonimo = await _context.VotosAnonimos.FindAsync(id);
            if (votoAnonimo == null)
            {
                return NotFound();
            }

            _context.VotosAnonimos.Remove(votoAnonimo);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool VotoAnonimoExists(int id)
        {
            return _context.VotosAnonimos.Any(e => e.Id == id);
        }
    }
}
