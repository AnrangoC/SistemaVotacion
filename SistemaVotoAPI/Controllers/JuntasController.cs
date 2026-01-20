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
            return await _context.Juntas
                .Include(j => j.Direccion)
                .Include(j => j.JefeDeJunta)
                .ToListAsync();
        }

        // GET: api/Juntas/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Junta>> GetJunta(int id)
        {
            var junta = await _context.Juntas
                .Include(j => j.Direccion)
                .Include(j => j.JefeDeJunta)
                .FirstOrDefaultAsync(j => j.Id == id);

            if (junta == null)
            {
                return NotFound();
            }

            return junta;
        }

        // PUT: api/Juntas/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        //
        // Este método NO debe usarse para editar:
        // - Número de mesa
        // - Dirección
        //
        // Solo se mantiene por el scaffold de Entity Framework.
        [HttpPut("{id}")]
        public async Task<IActionResult> PutJunta(int id, Junta junta)
        {
            if (id != junta.Id)
            {
                return BadRequest();
            }

            return BadRequest(
                "No está permitido modificar número de mesa ni dirección de una junta"
            );
        }

        // PUT: api/Juntas/AsignarJefe/5
        //
        // Asigna o cambia el jefe de junta
        // El jefe debe existir previamente como votante
        // Al asignarlo, su RolId se cambia automáticamente a 3
        [HttpPut("AsignarJefe/{juntaId}")]
        public async Task<IActionResult> AsignarJefeDeJunta(int juntaId, string cedulaVotante)
        {
            var junta = await _context.Juntas.FindAsync(juntaId);
            if (junta == null)
                return NotFound("Junta no encontrada");

            var votante = await _context.Votantes.FindAsync(cedulaVotante);
            if (votante == null)
                return NotFound("El votante no existe");

            // El votante no puede ser administrador ni candidato
            if (votante.RolId == 1)
                return BadRequest("Un administrador no puede ser jefe de junta");

            bool esCandidato = await _context.Candidatos
                .AnyAsync(c => c.Cedula == cedulaVotante);

            if (esCandidato)
                return BadRequest("Un candidato no puede ser jefe de junta");

            // Se asigna como jefe de junta
            junta.JefeDeJuntaId = cedulaVotante;

            // Se cambia el rol del votante a Jefe de Junta
            votante.RolId = 3;

            await _context.SaveChangesAsync();
            return Ok("Jefe de junta asignado correctamente");
        }

        // POST: api/Juntas
        //
        // La junta se crea una sola vez
        // Número de mesa y dirección quedan fijos
        [HttpPost]
        public async Task<ActionResult<Junta>> PostJunta(Junta junta)
        {
            _context.Juntas.Add(junta);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetJunta", new { id = junta.Id }, junta);
        }

        // DELETE: api/Juntas/5
        //
        // Normalmente no se elimina una junta,
        // se mantiene solo para control administrativo
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
