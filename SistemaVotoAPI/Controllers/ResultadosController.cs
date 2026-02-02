using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaVotoAPI.Data;
using SistemaVotoModelos;
using System;
using System.Collections.Generic;
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
        // GET: api/Resultados/Generales
        [HttpGet("Generales")]
        public async Task<IActionResult> ResultadosGenerales(int? eleccionId = null)
        {
            int eid;
            if (eleccionId.HasValue && eleccionId.Value > 0)
                eid = eleccionId.Value;
            else
            {
                var ultima = await _context.Elecciones.OrderByDescending(e => e.FechaInicio).FirstOrDefaultAsync();
                if (ultima == null) return NotFound("No existen elecciones.");
                eid = ultima.Id;
            }

            // Query global validando con Juntas Aprobadas (Estado 4)
            var query = from v in _context.VotosAnonimos
                        join j in _context.Juntas
                            on new { v.EleccionId, v.DireccionId, v.NumeroMesa }
                            equals new { j.EleccionId, j.DireccionId, j.NumeroMesa }
                        where v.EleccionId == eid && j.Estado == 4
                        select v;

            var conteo = await query
                .GroupBy(v => v.ListaId)
                .Select(g => new { ListaId = g.Key, TotalVotos = g.Count() })
                .ToListAsync();

            int granTotal = conteo.Sum(c => c.TotalVotos);
            var resultadosFinales = new List<object>();

            foreach (var item in conteo)
            {
                var lista = await _context.Listas.FindAsync(item.ListaId);
                double porcentaje = granTotal > 0 ? (double)item.TotalVotos / granTotal * 100 : 0;

                resultadosFinales.Add(new
                {
                    NombreLista = lista?.NombreLista ?? "Otros/Votos Nulos",
                    Logo = lista?.LogoUrl,
                    Votos = item.TotalVotos,
                    Porcentaje = Math.Round(porcentaje, 2)
                });
            }

            return Ok(new
            {
                EleccionId = eid,
                EsGeneral = true,
                TotalVotosGlobal = granTotal,
                Detalle = resultadosFinales.OrderByDescending(r => ((dynamic)r).Votos)
            });
        }
        // GET: api/Resultados/PorDireccion
        // Este método permite filtrar resultados por Provincia, Cantón y Parroquia
        [HttpGet("PorDireccion")]
        public async Task<IActionResult> ResultadosPorDireccion(
            string provincia,
            string? canton = null,
            string? parroquia = null,
            int? eleccionId = null)
        {
            if (string.IsNullOrWhiteSpace(provincia))
                return BadRequest("La provincia es obligatoria para realizar la búsqueda.");

            // Determinar la elección a consultar
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

                if (ultima == null) return NotFound("No existen elecciones registradas.");
                eid = ultima.Id;
            }

            //  Query con Joins para cruzar Votos, Direcciones y validar con Juntas Aprobadas (Estado 4)
            var query = from v in _context.VotosAnonimos
                        join d in _context.Direcciones on v.DireccionId equals d.Id
                        join j in _context.Juntas
                            on new { v.EleccionId, v.DireccionId, v.NumeroMesa }
                            equals new { j.EleccionId, j.DireccionId, j.NumeroMesa }
                        where v.EleccionId == eid && j.Estado == 4 && d.Provincia == provincia
                        select new { v, d };

            //  Aplicar filtros geográficos opcionales
            if (!string.IsNullOrWhiteSpace(canton))
                query = query.Where(x => x.d.Canton == canton);

            if (!string.IsNullOrWhiteSpace(parroquia))
                query = query.Where(x => x.d.Parroquia == parroquia);

            //  Agrupar por ListaId y contar los votos
            var conteo = await query
                .GroupBy(x => x.v.ListaId)
                .Select(g => new { ListaId = g.Key, TotalVotos = g.Count() })
                .ToListAsync();

            //  Preparar respuesta con nombres de listas, logos y porcentajes
            int granTotal = conteo.Sum(c => c.TotalVotos);
            var resultadosFinales = new List<object>();

            foreach (var item in conteo)
            {
                var lista = await _context.Listas.FindAsync(item.ListaId);
                double porcentaje = granTotal > 0 ? (double)item.TotalVotos / granTotal * 100 : 0;

                resultadosFinales.Add(new
                {
                    NombreLista = lista?.NombreLista ?? "Otros/Votos Nulos",
                    Logo = lista?.LogoUrl,
                    Votos = item.TotalVotos,
                    Porcentaje = Math.Round(porcentaje, 2)
                });
            }

            return Ok(new
            {
                EleccionId = eid,
                Filtros = new { Provincia = provincia, Canton = canton, Parroquia = parroquia },
                TotalVotosEnSector = granTotal,
                Detalle = resultadosFinales.OrderByDescending(r => ((dynamic)r).Votos)
            });
        }
    }
}