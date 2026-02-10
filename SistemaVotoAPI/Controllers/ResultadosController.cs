using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaVotoAPI.Data;
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
                var ultima = await _context.Elecciones
                    .OrderByDescending(e => e.FechaInicio)
                    .FirstOrDefaultAsync();

                if (ultima == null)
                    return NotFound("No existen elecciones.");

                eid = ultima.Id;
            }

            // Query validando juntas aprobadas
            var votos = from v in _context.VotosAnonimos
                        join j in _context.Juntas
                        on new { v.EleccionId, v.DireccionId, v.NumeroMesa }
                        equals new { j.EleccionId, j.DireccionId, j.NumeroMesa }
                        where v.EleccionId == eid && j.Estado == 4
                        select v;

            // Agrupar por Rol + Lista
            var conteo = await votos
                .GroupBy(v => new { v.RolPostulante, v.ListaId })
                .Select(g => new
                {
                    Rol = g.Key.RolPostulante,
                    ListaId = g.Key.ListaId,
                    TotalVotos = g.Count()
                })
                .ToListAsync();

            // Agrupar por Rol para armar secciones
            var roles = conteo
                .GroupBy(x => x.Rol)
                .Select(gr => new
                {
                    Rol = string.IsNullOrWhiteSpace(gr.Key) ? "SIN CARGO" : gr.Key,
                    TotalRol = gr.Sum(x => x.TotalVotos),
                    Detalle = gr.Select(x => new
                    {
                        ListaId = x.ListaId,
                        Votos = x.TotalVotos
                    }).ToList()
                })
                .ToList();

            // Armar respuesta final con nombres de listas
            var resultadoFinal = new List<object>();

            foreach (var rol in roles)
            {
                var detalleFinal = new List<object>();

                foreach (var item in rol.Detalle)
                {
                    var lista = await _context.Listas.FindAsync(item.ListaId);

                    double porcentaje = rol.TotalRol > 0
                        ? (double)item.Votos / rol.TotalRol * 100
                        : 0;

                    detalleFinal.Add(new
                    {
                        NombreLista = lista?.NombreLista ?? "Blanco",
                        Logo = lista?.LogoUrl,
                        Votos = item.Votos,
                        Porcentaje = Math.Round(porcentaje, 2)
                    });
                }

                resultadoFinal.Add(new
                {
                    Rol = rol.Rol,
                    TotalRol = rol.TotalRol,
                    Detalle = detalleFinal.OrderByDescending(x => ((dynamic)x).Votos)
                });
            }

            return Ok(new
            {
                EleccionId = eid,
                Roles = resultadoFinal
            });
        }

        // GET: api/Resultados/PorDireccion
        [HttpGet("PorDireccion")]
        public async Task<IActionResult> ResultadosPorDireccion(
            string provincia,
            string? canton = null,
            string? parroquia = null,
            int? eleccionId = null)
        {
            if (string.IsNullOrWhiteSpace(provincia))
                return BadRequest("Provincia obligatoria.");

            int eid;

            if (eleccionId.HasValue && eleccionId.Value > 0)
                eid = eleccionId.Value;
            else
            {
                var ultima = await _context.Elecciones
                    .OrderByDescending(e => e.FechaInicio)
                    .FirstOrDefaultAsync();

                if (ultima == null)
                    return NotFound("No existen elecciones.");

                eid = ultima.Id;
            }

            var votos = from v in _context.VotosAnonimos
                        join d in _context.Direcciones on v.DireccionId equals d.Id
                        join j in _context.Juntas
                        on new { v.EleccionId, v.DireccionId, v.NumeroMesa }
                        equals new { j.EleccionId, j.DireccionId, j.NumeroMesa }
                        where v.EleccionId == eid
                        && j.Estado == 4
                        && d.Provincia == provincia
                        select new { v, d };

            if (!string.IsNullOrWhiteSpace(canton))
                votos = votos.Where(x => x.d.Canton == canton);

            if (!string.IsNullOrWhiteSpace(parroquia))
                votos = votos.Where(x => x.d.Parroquia == parroquia);

            var conteo = await votos
                .GroupBy(x => new { x.v.RolPostulante, x.v.ListaId })
                .Select(g => new
                {
                    Rol = g.Key.RolPostulante,
                    ListaId = g.Key.ListaId,
                    TotalVotos = g.Count()
                })
                .ToListAsync();

            var roles = conteo
                .GroupBy(x => x.Rol)
                .Select(gr => new
                {
                    Rol = string.IsNullOrWhiteSpace(gr.Key) ? "SIN CARGO" : gr.Key,
                    TotalRol = gr.Sum(x => x.TotalVotos),
                    Detalle = gr.ToList()
                })
                .ToList();

            var resultadoFinal = new List<object>();

            foreach (var rol in roles)
            {
                var detalleFinal = new List<object>();

                foreach (var item in rol.Detalle)
                {
                    var lista = await _context.Listas.FindAsync(item.ListaId);

                    double porcentaje = rol.TotalRol > 0
                        ? (double)item.TotalVotos / rol.TotalRol * 100
                        : 0;

                    detalleFinal.Add(new
                    {
                        NombreLista = lista?.NombreLista ?? "Blanco",
                        Logo = lista?.LogoUrl,
                        Votos = item.TotalVotos,
                        Porcentaje = Math.Round(porcentaje, 2)
                    });
                }

                resultadoFinal.Add(new
                {
                    Rol = rol.Rol,
                    TotalRol = rol.TotalRol,
                    Detalle = detalleFinal.OrderByDescending(x => ((dynamic)x).Votos)
                });
            }

            return Ok(new
            {
                EleccionId = eid,
                Filtros = new { Provincia = provincia, Canton = canton, Parroquia = parroquia },
                Roles = resultadoFinal
            });
        }
    }
}
