using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaVotoAPI.Data;
using System.Linq;
using System.Threading.Tasks;

namespace SistemaVotoAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ResultadosController : ControllerBase
    {
        private readonly APIVotosDbContext _context;

        public ResultadosController(APIVotosDbContext context)
        {
            _context = context;
        }

        // Estados Junta (int)
        // 1=Cerrada | 2=Abierta | 3=Pendiente de aprobación | 4=Aprobada

        // RESULTADOS POR JUNTA (solo si está aprobada)
        [HttpGet("PorJunta/{juntaId:int}")]
        public async Task<IActionResult> ResultadosPorJunta(int juntaId)
        {
            var junta = await _context.Juntas.FindAsync(juntaId);
            if (junta == null)
                return NotFound("Junta no encontrada.");

            if (junta.Estado != 4)
                return Conflict("Aún no hay resultados: la junta no está aprobada.");

            var resultados = await _context.VotosAnonimos
                .Where(v =>
                    v.EleccionId == junta.EleccionId &&
                    v.DireccionId == junta.DireccionId &&
                    v.NumeroMesa == junta.NumeroMesa
                )
                .GroupBy(v => v.ListaId)
                .Select(g => new
                {
                    ListaId = g.Key,
                    TotalVotos = g.Count()
                })
                .ToListAsync();

            return Ok(resultados);
        }

        // RESULTADOS POR LISTA (solo votos que provienen de juntas aprobadas)
        [HttpGet("PorLista/{listaId:int}")]
        public async Task<IActionResult> ResultadosPorLista(int listaId)
        {
            var lista = await _context.Listas.FindAsync(listaId);
            if (lista == null)
                return NotFound("Lista no encontrada.");

            int eleccionId = lista.EleccionId;

            // Solo cuenta votos de juntas aprobadas (Estado=4)
            var total = await (
                from v in _context.VotosAnonimos
                join j in _context.Juntas
                    on new { v.EleccionId, v.DireccionId, v.NumeroMesa }
                    equals new { j.EleccionId, j.DireccionId, j.NumeroMesa }
                where j.Estado == 4
                      && v.EleccionId == eleccionId
                      && v.ListaId == listaId
                select v
            ).CountAsync();

            return Ok(new
            {
                ListaId = listaId,
                TotalVotos = total
            });
        }

        // RESULTADOS POR DIRECCIÓN (solo votos de juntas aprobadas)
        [HttpGet("PorDireccion")]
        public async Task<IActionResult> ResultadosPorDireccion(
            string provincia,
            string? canton = null,
            string? parroquia = null,
            int? eleccionId = null
        )
        {
            if (string.IsNullOrWhiteSpace(provincia))
                return BadRequest("Provincia obligatoria.");

            // Si no mandan elección, tomo la más reciente por FechaInicio
            int eid;
            if (eleccionId.HasValue && eleccionId.Value > 0)
            {
                eid = eleccionId.Value;
            }
            else
            {
                var ultima = await _context.Elecciones
                    .OrderByDescending(e => e.FechaInicio)
                    .FirstOrDefaultAsync();

                if (ultima == null)
                    return NotFound("No existen elecciones registradas.");

                eid = ultima.Id;
            }

            var query =
                from v in _context.VotosAnonimos
                join d in _context.Direcciones on v.DireccionId equals d.Id
                join j in _context.Juntas
                    on new { v.EleccionId, v.DireccionId, v.NumeroMesa }
                    equals new { j.EleccionId, j.DireccionId, j.NumeroMesa }
                where v.EleccionId == eid
                      && j.Estado == 4
                      && d.Provincia == provincia
                select new { v, d };

            if (!string.IsNullOrWhiteSpace(canton))
                query = query.Where(x => x.d.Canton == canton);

            if (!string.IsNullOrWhiteSpace(parroquia))
                query = query.Where(x => x.d.Parroquia == parroquia);

            var resultados = await query
                .GroupBy(x => x.v.ListaId)
                .Select(g => new
                {
                    ListaId = g.Key,
                    TotalVotos = g.Count()
                })
                .ToListAsync();

            return Ok(new
            {
                EleccionId = eid,
                Provincia = provincia,
                Canton = canton,
                Parroquia = parroquia,
                Resultados = resultados
            });
        }

        // VALIDACIÓN PARA CIERRE DE JUNTA
        // Aquí no obligo que esté aprobada, porque esto es para que el admin compare
        [HttpGet("ValidarCierreJunta/{juntaId:int}")]
        public async Task<IActionResult> ValidarCierreJunta(int juntaId)
        {
            var junta = await _context.Juntas.FindAsync(juntaId);
            if (junta == null)
                return NotFound("Junta no encontrada.");

            var totalVotantes = await _context.Votantes
                .CountAsync(v => v.JuntaId == juntaId);

            var totalVotos = await _context.VotosAnonimos
                .CountAsync(v =>
                    v.EleccionId == junta.EleccionId &&
                    v.DireccionId == junta.DireccionId &&
                    v.NumeroMesa == junta.NumeroMesa
                );

            return Ok(new
            {
                JuntaId = juntaId,
                NumeroMesa = junta.NumeroMesa,
                TotalVotantes = totalVotantes,
                TotalVotosEmitidos = totalVotos,
                Coinciden = totalVotantes == totalVotos
            });
        }
    }
}
