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
    public class ListasController : ControllerBase
    {
        private readonly APIVotosDbContext _context;

        public ListasController(APIVotosDbContext context)
        {
            _context = context;
        }

        // GET: api/Listas
        //
        // Devuelve todas las listas registradas en el sistema
        // Normalmente se usan para:
        // - Mostrar opciones de votación
        // - Mostrar resultados
        // - Gestión administrativa
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Lista>>> GetLista()
        {
            return await _context.Listas.ToListAsync();
        }

        // GET: api/Listas/5
        //
        // Obtiene una lista específica por su Id
        [HttpGet("{id}")]
        public async Task<ActionResult<Lista>> GetLista(int id)
        {
            var lista = await _context.Listas.FindAsync(id);

            if (lista == null)
            {
                return NotFound();
            }

            return lista;
        }

        // GET: api/Listas/PorEleccion/3
        //
        // Devuelve todas las listas asociadas a una elección específica
        // Se usa para:
        // - Mostrar listas disponibles al votar
        // - Mostrar resultados por elección
        [HttpGet("PorEleccion/{eleccionId}")]
        public async Task<ActionResult<IEnumerable<Lista>>> GetListasPorEleccion(int eleccionId)
        {
            return await _context.Listas
                .Where(l => l.EleccionId == eleccionId)
                .ToListAsync();
        }

        // PUT: api/Listas/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        //
        // Permite editar únicamente datos visuales de la lista
        // (nombre, logo).
        // La lógica de candidatos NO se maneja aquí.
        [HttpPut("{id}")]
        public async Task<IActionResult> PutLista(int id, Lista lista)
        {
            if (id != lista.Id)
            {
                return BadRequest();
            }

            _context.Entry(lista).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ListaExists(id))
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

        // POST: api/Listas
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        //
        // Crea una nueva lista dentro de una elección
        // La lista no tiene candidatos al crearse
        [HttpPost]
        public async Task<ActionResult<Lista>> PostLista(Lista lista)
        {
            _context.Listas.Add(lista);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetLista),
                new { id = lista.Id },
                lista
            );
        }

        // DELETE: api/Listas/5
        //
        // Normalmente una lista NO se elimina si ya tiene candidatos
        // Este método se mantiene para control administrativo
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLista(int id)
        {
            var lista = await _context.Listas.FindAsync(id);
            if (lista == null)
            {
                return NotFound();
            }

            _context.Listas.Remove(lista);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private  bool ListaExists(int id)
        {
            return _context.Listas.Any(e => e.Id == id);
        }
    }
}
