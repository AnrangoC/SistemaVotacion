using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaVotoAPI.Data;
using SistemaVotoModelos;

namespace SistemaVotoAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CandidatosController : ControllerBase
    {
        private readonly APIVotosDbContext _context;

        public CandidatosController(APIVotosDbContext context)
        {
            _context = context;
        }

        // GET: api/Candidatos
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Candidato>>> GetCandidato()
        {
            return await _context.Candidatos.ToListAsync();
        }

        // GET: api/Candidatos/0102030405
        [HttpGet("{cedula}")]
        public async Task<ActionResult<Candidato>> GetCandidato(string cedula)
        {
            var candidato = await _context.Candidatos.FindAsync(cedula);

            if (candidato == null)
            {
                return NotFound();
            }

            return candidato;
        }

        // PUT: api/Candidatos/0102030405
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{cedula}")]
        public async Task<IActionResult> PutCandidato(string cedula, Candidato candidato)
        {
            if (cedula != candidato.Cedula)
            {
                return BadRequest();
            }

            _context.Entry(candidato).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CandidatoExists(cedula))
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

        // POST: api/Candidatos
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Candidato>> PostCandidato(Candidato candidato)
        {
            if (await _context.Candidatos.AnyAsync(c => c.Cedula == candidato.Cedula))
                return Conflict("Ya existe un candidato con esa cédula.");

            _context.Candidatos.Add(candidato);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetCandidato),
                new { cedula = candidato.Cedula },
                candidato
            );
        }

        // DELETE: api/Candidatos/0102030405
        [HttpDelete("{cedula}")]
        public async Task<IActionResult> DeleteCandidato(string cedula)
        {
            var candidato = await _context.Candidatos.FindAsync(cedula);
            if (candidato == null)
            {
                return NotFound();
            }

            _context.Candidatos.Remove(candidato);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool CandidatoExists(string cedula)
        {
            return _context.Candidatos.Any(e => e.Cedula == cedula);
        }
    }
}
