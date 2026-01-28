using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaVotoModelos;
using SistemaVotoModelos.DTOs;
using System.Net.Http.Json;

namespace SistemaVotoMVC.Controllers
{
    [Authorize(Roles = "1")]
    public class JuntasController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        private readonly string _endpointJuntas = "api/Juntas";
        private readonly string _endpointDirecciones = "api/Direcciones";
        private readonly string _endpointElecciones = "api/Elecciones";

        public JuntasController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");

            // 1) Juntas
            var respJ = await client.GetAsync(_endpointJuntas);
            var juntas = respJ.IsSuccessStatusCode
                ? await respJ.Content.ReadFromJsonAsync<List<JuntaDetalleDto>>() ?? new List<JuntaDetalleDto>()
                : new List<JuntaDetalleDto>();

            if (!respJ.IsSuccessStatusCode)
            {
                var apiMsg = await respJ.Content.ReadAsStringAsync();
                TempData["Error"] = $"No se pudo obtener juntas desde la API. Código: {(int)respJ.StatusCode}. {apiMsg}";
            }

            // 2) Direcciones para el modal
            var respD = await client.GetAsync(_endpointDirecciones);
            var direcciones = respD.IsSuccessStatusCode
                ? await respD.Content.ReadFromJsonAsync<List<Direccion>>() ?? new List<Direccion>()
                : new List<Direccion>();

            if (!respD.IsSuccessStatusCode)
            {
                var apiMsg = await respD.Content.ReadAsStringAsync();
                TempData["Error"] = $"No se pudo obtener direcciones. Código: {(int)respD.StatusCode}. {apiMsg}";
            }

            // 3) Elecciones para el modal
            var respE = await client.GetAsync(_endpointElecciones);
            var elecciones = respE.IsSuccessStatusCode
                ? await respE.Content.ReadFromJsonAsync<List<Eleccion>>() ?? new List<Eleccion>()
                : new List<Eleccion>();

            if (!respE.IsSuccessStatusCode)
            {
                var apiMsg = await respE.Content.ReadAsStringAsync();
                TempData["Error"] = $"No se pudo obtener elecciones. Código: {(int)respE.StatusCode}. {apiMsg}";
            }

            ViewBag.Direcciones = direcciones.OrderBy(d => d.Id).ToList();
            ViewBag.Elecciones = elecciones.OrderByDescending(e => e.Id).ToList();

            return View(juntas);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearPorDireccion(int eleccionId, int direccionId, int cantidad)
        {
            if (eleccionId <= 0 || direccionId <= 0 || cantidad <= 0)
            {
                TempData["Error"] = "Elección, dirección o cantidad inválida.";
                return RedirectToAction(nameof(Index));
            }

            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");

            var resp = await client.PostAsync(
                $"{_endpointJuntas}/CrearPorDireccion?eleccionId={eleccionId}&direccionId={direccionId}&cantidad={cantidad}",
                null
            );

            if (resp.IsSuccessStatusCode)
            {
                TempData["Mensaje"] = "Juntas creadas correctamente.";
                return RedirectToAction(nameof(Index));
            }

            var apiMsg = await resp.Content.ReadAsStringAsync();
            TempData["Error"] = string.IsNullOrWhiteSpace(apiMsg) ? "No se pudo crear juntas." : apiMsg;

            return RedirectToAction(nameof(Index));
        }
    }
}
