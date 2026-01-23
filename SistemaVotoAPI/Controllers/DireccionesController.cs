using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniExcelLibs;
using SistemaVotoAPI.Data;
using SistemaVotoModelos;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SistemaVotoAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DireccionesController : ControllerBase
    {
        private readonly APIVotosDbContext _context;

        public DireccionesController(APIVotosDbContext context)
        {
            _context = context;
        }

        [HttpPost("CargaMasiva")]
        public async Task<IActionResult> CargaMasiva(IFormFile archivo)
        {
            if (archivo == null || archivo.Length == 0)
                return BadRequest("Archivo no proporcionado.");

            using var stream = archivo.OpenReadStream();
            var filas = stream.Query(useHeaderRow: true);

            var provincias = new Dictionary<string, int>();
            var cantones = new Dictionary<(string prov, string cant), int>();
            var parroquias = new Dictionary<(string prov, string cant, string parr), int>();

            var contadorCantones = new Dictionary<string, int>();
            var contadorParroquias = new Dictionary<(string prov, string cant), int>();

            int provinciaSeq = 1;

            foreach (var fila in filas)
            {
                string provincia = fila.Provincia?.ToString().Trim();
                string canton = fila.Canton?.ToString().Trim();
                string parroquia = fila.Parroquia?.ToString().Trim();

                if (string.IsNullOrWhiteSpace(provincia) ||
                    string.IsNullOrWhiteSpace(canton) ||
                    string.IsNullOrWhiteSpace(parroquia))
                    continue;

                // PROVINCIA
                if (!provincias.ContainsKey(provincia))
                {
                    provincias[provincia] = provinciaSeq++;
                    contadorCantones[provincia] = 1;
                }

                int provId = provincias[provincia];

                // CANTÓN (por provincia)
                var cantKey = (provincia, canton);
                if (!cantones.ContainsKey(cantKey))
                {
                    cantones[cantKey] = contadorCantones[provincia]++;
                    contadorParroquias[cantKey] = 1;
                }

                int cantId = cantones[cantKey];

                // PARROQUIA (por cantón)
                var parrKey = (provincia, canton, parroquia);
                if (!parroquias.ContainsKey(parrKey))
                {
                    parroquias[parrKey] = contadorParroquias[cantKey]++;
                }

                int parrId = parroquias[parrKey];

                int direccionId = int.Parse($"{provId:D2}{cantId:D2}{parrId:D2}");

                bool existe = await _context.Direcciones
                    .AnyAsync(d => d.Id == direccionId);

                if (!existe)
                {
                    _context.Direcciones.Add(new Direccion
                    {
                        Id = direccionId,
                        Provincia = provincia,
                        Canton = canton,
                        Parroquia = parroquia
                    });
                }
            }

            await _context.SaveChangesAsync();
            return Ok("Carga masiva de direcciones completada.");
        }
    }
}
