using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaVotoAPI.Data;
using SistemaVotoModelos;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SistemaVotoAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VotantesController : ControllerBase
    {
        private readonly APIVotosDbContext _context;

        public VotantesController(APIVotosDbContext context)
        {
            _context = context;
        }

        // GET: api/Votantes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Votante>>> GetVotantes()
        {
            return await _context.Votantes.ToListAsync();
        }

        // GET: api/Votantes/0102030405
        [HttpGet("{cedula}")]
        public async Task<ActionResult<Votante>> GetVotante(string cedula)
        {
            var votante = await _context.Votantes.FindAsync(cedula);

            if (votante == null)
                return NotFound();

            return votante;
        }

        // GET: api/Votantes/PorJunta/3
        [HttpGet("PorJunta/{juntaId}")]
        public async Task<ActionResult<IEnumerable<Votante>>> GetVotantesPorJunta(int juntaId)
        {
            return await _context.Votantes
                .Where(v => v.JuntaId == juntaId)
                .ToListAsync();
        }

        // POST: api/Votantes
        [HttpPost]
        public async Task<ActionResult<Votante>> PostVotante(Votante votante)
        {
            if (await _context.Votantes.AnyAsync(v => v.Cedula == votante.Cedula))
                return Conflict("Ya existe un votante con esa cédula.");

            _context.Votantes.Add(votante);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetVotante), new { cedula = votante.Cedula }, votante);
        }

        // PUT: api/Votantes/0102030405
        [HttpPut("{cedula}")]
        public async Task<IActionResult> PutVotante(string cedula, Votante votante)
        {
            if (cedula != votante.Cedula)
                return BadRequest();

            _context.Entry(votante).State = EntityState.Modified;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/Votantes/0102030405
        [HttpDelete("{cedula}")]
        public async Task<IActionResult> DeleteVotante(string cedula)
        {
            var votante = await _context.Votantes.FindAsync(cedula);
            if (votante == null)
                return NotFound();

            _context.Votantes.Remove(votante);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}