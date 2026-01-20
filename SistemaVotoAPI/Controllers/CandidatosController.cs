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
        public async Task<ActionResult<IEnumerable<Candidato>>> GetCandidatos()
        {
            return await _context.Candidatos.ToListAsync();
        }

        // GET: api/Candidatos/0102030405
        [HttpGet("{cedula}")]
        public async Task<ActionResult<Candidato>> GetCandidato(string cedula)
        {
            var candidato = await _context.Candidatos.FindAsync(cedula);

            if (candidato == null)
                return NotFound();

            return candidato;
        }

        // POST: api/Candidatos
        // Registra un candidato a partir de un votante existente
        [HttpPost]
        public async Task<ActionResult<Candidato>> PostCandidato(Candidato candidato)
        {
            // Verificar que el votante exista
            var votante = await _context.Votantes.FindAsync(candidato.Cedula);
            if (votante == null)
                return BadRequest("El votante no existe");

            // Verificar que no sea admin ni jefe de junta
            if (votante.RolId == 1 || votante.RolId == 3)
                return Conflict("Un administrador o jefe de junta no puede ser candidato");

            // Verificar que no exista ya como candidato
            bool yaEsCandidato = await _context.Candidatos
                .AnyAsync(c => c.Cedula == candidato.Cedula);

            if (yaEsCandidato)
                return Conflict("El votante ya es candidato a otro cargo");

            // Verificar que tenga lista asignada
            bool listaExiste = await _context.Listas
                .AnyAsync(l => l.Id == candidato.ListaId);

            if (!listaExiste)
                return BadRequest("La lista asignada no existe");

            // Registrar como candidato
            _context.Candidatos.Add(candidato);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetCandidato),
                new { cedula = candidato.Cedula },
                candidato
            );
        }

        // PUT: api/Candidatos/0102030405
        // Permite cambiar lista o cargo del candidato
        [HttpPut("{cedula}")]
        public async Task<IActionResult> PutCandidato(string cedula, Candidato datosActualizados)
        {
            if (cedula != datosActualizados.Cedula)
                return BadRequest();

            var candidato = await _context.Candidatos.FindAsync(cedula);
            if (candidato == null)
                return NotFound();

            // Validar que la nueva lista exista
            bool listaExiste = await _context.Listas
                .AnyAsync(l => l.Id == datosActualizados.ListaId);

            if (!listaExiste)
                return BadRequest("La lista asignada no existe");

            // Actualizar solo los atributos de candidato
            candidato.ListaId = datosActualizados.ListaId;
            candidato.RolPostulante = datosActualizados.RolPostulante;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/Candidatos/0102030405
        // Elimina solo el rol de candidato, el votante se conserva
        [HttpDelete("{cedula}")]
        public async Task<IActionResult> DeleteCandidato(string cedula)
        {
            var candidato = await _context.Candidatos.FindAsync(cedula);
            if (candidato == null)
                return NotFound();

            _context.Candidatos.Remove(candidato);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
