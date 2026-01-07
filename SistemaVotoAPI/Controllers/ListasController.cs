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
    public class ListasController : ControllerBase
    {
        private readonly APIVotosDbContext _context;

        public ListasController(APIVotosDbContext context)
        {
            _context = context;
        }

        // GET: api/Listas
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Lista>>> GetLista()
        {
            return await _context.Listas.ToListAsync();
        }

        // GET: api/Listas/5
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

        // PUT: api/Listas/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
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
        [HttpPost]
        public async Task<ActionResult<Lista>> PostLista(Lista lista)
        {
            _context.Listas.Add(lista);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetLista", new { id = lista.Id }, lista);
        }

        // DELETE: api/Listas/5
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

        private bool ListaExists(int id)
        {
            return _context.Listas.Any(e => e.Id == id);
        }
    }
}
