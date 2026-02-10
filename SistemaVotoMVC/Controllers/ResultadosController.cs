using Microsoft.AspNetCore.Mvc;
using SistemaVotoModelos;
using System.Net.Http.Json;

namespace SistemaVotoMVC.Controllers
{
    public class ResultadosController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ResultadosController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // Carga resultados generales o filtrados
        [HttpGet]
        public async Task<IActionResult> Index(string? provincia, string? canton, string? parroquia, int? eleccionId)
        {
            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");

            // Obtener lista de elecciones
            var respElecciones = await client.GetAsync("api/Elecciones");

            var elecciones = respElecciones.IsSuccessStatusCode
                ? await respElecciones.Content.ReadFromJsonAsync<List<Eleccion>>() ?? new List<Eleccion>()
                : new List<Eleccion>();

            ViewBag.Elecciones = elecciones;
            ViewBag.EleccionSeleccionada = eleccionId;

            // Determinar endpoint
            string url;

            if (string.IsNullOrWhiteSpace(provincia))
            {
                url = $"api/Resultados/Generales?eleccionId={eleccionId}";
                ViewBag.TituloVista = "Resultados por Cargo (General)";
            }
            else
            {
                url = $"api/Resultados/PorDireccion?provincia={provincia}&canton={canton}&parroquia={parroquia}&eleccionId={eleccionId}";
                ViewBag.TituloVista = $"Resultados por Cargo en {provincia}";
            }

            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "No se pudieron obtener los resultados.";
                return View(null);
            }

            // Leer nuevo JSON con Roles[]
            var resultados = await response.Content.ReadFromJsonAsync<dynamic>();

            return View(resultados);
        }

        // Endpoint AJAX para gráficos dinámicos
        [HttpGet]
        public async Task<IActionResult> ObtenerJson(string? provincia, string? canton, string? parroquia, int? eleccionId)
        {
            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");

            string url = string.IsNullOrWhiteSpace(provincia)
                ? $"api/Resultados/Generales?eleccionId={eleccionId}"
                : $"api/Resultados/PorDireccion?provincia={provincia}&canton={canton}&parroquia={parroquia}&eleccionId={eleccionId}";

            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return BadRequest();

            var data = await response.Content.ReadAsStringAsync();
            return Content(data, "application/json");
        }
    }
}
