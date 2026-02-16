using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaVotoAPI.Data;
using System;
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

            // OJO: ya NO filtramos por CedulaCandidato, para incluir BLANCO
            var votosValidos =
                from v in _context.VotosAnonimos
                join j in _context.Juntas
                    on new { v.EleccionId, v.DireccionId, v.NumeroMesa }
                    equals new { j.EleccionId, j.DireccionId, j.NumeroMesa }
                where v.EleccionId == eid
                      && j.Estado == 4
                select v;

            // Conteo por Rol + (CedulaCandidato o BLANCO)
            var conteo = await votosValidos
                .GroupBy(v => new
                {
                    v.RolPostulante,
                    CedulaKey = string.IsNullOrWhiteSpace(v.CedulaCandidato) ? "BLANCO" : v.CedulaCandidato
                })
                .Select(g => new
                {
                    Rol = g.Key.RolPostulante,
                    CedulaCandidato = g.Key.CedulaKey,
                    TotalVotos = g.Count()
                })
                .ToListAsync();

            // Candidatos de ESTA elección con Votante y Lista
            var candidatos = await _context.Candidatos
                .Include(c => c.Votante)
                .Include(c => c.Lista)
                .Where(c => c.EleccionId == eid)
                .ToListAsync();

            var candidatosDict = candidatos
                .GroupBy(c => c.Cedula)
                .ToDictionary(g => g.Key, g => g.First());

            var resultadoFinal = conteo
                .GroupBy(x => x.Rol)
                .Select(gr =>
                {
                    var rolNombre = string.IsNullOrWhiteSpace(gr.Key) ? "SIN CARGO" : gr.Key;
                    var totalRol = gr.Sum(x => x.TotalVotos);

                    var detalle = gr.Select(x =>
                    {
                        var esBlanco = x.CedulaCandidato == "BLANCO";

                        candidatosDict.TryGetValue(x.CedulaCandidato, out var cand);

                        double porcentaje = totalRol > 0
                            ? (double)x.TotalVotos / totalRol * 100
                            : 0;

                        return new
                        {
                            CedulaCandidato = esBlanco ? "" : x.CedulaCandidato,
                            NombrePostulante = esBlanco
                                ? "Blanco"
                                : (cand?.Votante?.NombreCompleto ?? "CANDIDATO NO ENCONTRADO"),
                            FotoPostulante = esBlanco ? null : cand?.Votante?.FotoUrl,
                            ListaId = esBlanco ? 0 : (cand?.ListaId ?? 0),
                            NombreLista = esBlanco ? "Blanco" : (cand?.Lista?.NombreLista ?? ""),
                            Logo = esBlanco ? null : cand?.Lista?.LogoUrl,
                            Votos = x.TotalVotos,
                            Porcentaje = Math.Round(porcentaje, 2)
                        };
                    })
                    .OrderByDescending(d => d.Votos)
                    .ToList();

                    return new
                    {
                        Rol = rolNombre,
                        TotalRol = totalRol,
                        Ganador = detalle.FirstOrDefault(),
                        Detalle = detalle
                    };
                })
                .ToList();

            return Ok(new
            {
                EleccionId = eid,
                Roles = resultadoFinal
            });
        }

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

            //
            var votosValidos =
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
                votosValidos = votosValidos.Where(x => x.d.Canton == canton);

            if (!string.IsNullOrWhiteSpace(parroquia))
                votosValidos = votosValidos.Where(x => x.d.Parroquia == parroquia);

            var conteo = await votosValidos
                .GroupBy(x => new
                {
                    x.v.RolPostulante,
                    CedulaKey = string.IsNullOrWhiteSpace(x.v.CedulaCandidato) ? "BLANCO" : x.v.CedulaCandidato
                })
                .Select(g => new
                {
                    Rol = g.Key.RolPostulante,
                    CedulaCandidato = g.Key.CedulaKey,
                    TotalVotos = g.Count()
                })
                .ToListAsync();

            var candidatos = await _context.Candidatos
                .Include(c => c.Votante)
                .Include(c => c.Lista)
                .Where(c => c.EleccionId == eid)
                .ToListAsync();

            var candidatosDict = candidatos
                .GroupBy(c => c.Cedula)
                .ToDictionary(g => g.Key, g => g.First());

            var resultadoFinal = conteo
                .GroupBy(x => x.Rol)
                .Select(gr =>
                {
                    var rolNombre = string.IsNullOrWhiteSpace(gr.Key) ? "SIN CARGO" : gr.Key;
                    var totalRol = gr.Sum(x => x.TotalVotos);

                    var detalle = gr.Select(x =>
                    {
                        var esBlanco = x.CedulaCandidato == "BLANCO";

                        candidatosDict.TryGetValue(x.CedulaCandidato, out var cand);

                        double porcentaje = totalRol > 0
                            ? (double)x.TotalVotos / totalRol * 100
                            : 0;

                        return new
                        {
                            CedulaCandidato = esBlanco ? "" : x.CedulaCandidato,
                            NombrePostulante = esBlanco
                                ? "Blanco"
                                : (cand?.Votante?.NombreCompleto ?? "CANDIDATO NO ENCONTRADO"),
                            FotoPostulante = esBlanco ? null : cand?.Votante?.FotoUrl,
                            ListaId = esBlanco ? 0 : (cand?.ListaId ?? 0),
                            NombreLista = esBlanco ? "Blanco" : (cand?.Lista?.NombreLista ?? ""),
                            Logo = esBlanco ? null : cand?.Lista?.LogoUrl,
                            Votos = x.TotalVotos,
                            Porcentaje = Math.Round(porcentaje, 2)
                        };
                    })
                    .OrderByDescending(d => d.Votos)
                    .ToList();

                    return new
                    {
                        Rol = rolNombre,
                        TotalRol = totalRol,
                        Ganador = detalle.FirstOrDefault(),
                        Detalle = detalle
                    };
                })
                .ToList();

            return Ok(new
            {
                EleccionId = eid,
                Filtros = new { Provincia = provincia, Canton = canton, Parroquia = parroquia },
                Roles = resultadoFinal
            });
        }
    }
}
