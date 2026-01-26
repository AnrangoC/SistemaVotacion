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
    public class VotosAnonimosController : ControllerBase
    {
        private readonly APIVotosDbContext _context;

        public VotosAnonimosController(APIVotosDbContext context)
        {
            _context = context;
        }

        // GET: api/VotosAnonimos
        // Este endpoint es solo para uso administrativo y reportes
        // NO debe usarse para exponer votos individualmente al público
        [HttpGet]
        public async Task<ActionResult<IEnumerable<VotoAnonimo>>> GetVotosAnonimos()
        {
            return await _context.VotosAnonimos.ToListAsync();
        }

        // POST: api/VotosAnonimos
        // Registra un voto anónimo
        // No se guarda información del votante
        // El vínculo con el votante se rompe aquí
        [HttpPost]
        public async Task<ActionResult<VotoAnonimo>> PostVotoAnonimo(VotoAnonimo votoAnonimo)
        {
            /*
             Validaciones mínimas obligatorias:
              Elección válida
              Dirección válida
              Número de mesa válido
            */

            bool eleccionExiste = await _context.Elecciones
                .AnyAsync(e => e.Id == votoAnonimo.EleccionId);

            if (!eleccionExiste)
                return BadRequest("Elección no válida");

            votoAnonimo.FechaVoto = DateTime.UtcNow;

            _context.VotosAnonimos.Add(votoAnonimo);
            await _context.SaveChangesAsync();

            return Ok("Voto registrado correctamente");
        }

        /*
         El voto solo se crea no se puede ni editar ni eliminar y no se tiene que almacenar con un id del votante
         
        */
    }
}
