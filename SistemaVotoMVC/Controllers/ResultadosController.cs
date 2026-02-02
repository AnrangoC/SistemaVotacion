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

        // Carga resultados generales por defecto o filtrados si hay parámetros
        [HttpGet]
        public async Task<IActionResult> Index(string? provincia, string? canton, string? parroquia, int? eleccionId)
        {
            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");

            //  Obtener lista de elecciones para el dropdown de filtros
            var respElecciones = await client.GetAsync("api/Elecciones");
            var elecciones = respElecciones.IsSuccessStatusCode
                ? await respElecciones.Content.ReadFromJsonAsync<List<Eleccion>>() ?? new List<Eleccion>()
                : new List<Eleccion>();

            ViewBag.Elecciones = elecciones;
            ViewBag.EleccionSeleccionada = eleccionId;

            // Determinar qué endpoint llamar
            string url;
            if (string.IsNullOrWhiteSpace(provincia))
            {
                // Si no hay provincia, pedimos el consolidado GENERAL
                url = $"api/Resultados/Generales?eleccionId={eleccionId}";
                ViewBag.TituloVista = "Resultados Generales (Consolidado)";
            }
            else
            {
                // Si hay provincia, pedimos el detalle por DIRECCIÓN
                url = $"api/Resultados/PorDireccion?provincia={provincia}&canton={canton}&parroquia={parroquia}&eleccionId={eleccionId}";
                ViewBag.TituloVista = $"Resultados en {provincia} {(string.IsNullOrEmpty(canton) ? "" : " / " + canton)}";
            }

            // Obtener los datos (Votos, Porcentajes, Detalle de Listas)
            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "No se pudieron obtener los resultados en este momento.";
                return View(null);
            }

            // Usamos dynamic para recibir el objeto JSON complejo de la API
            var resultados = await response.Content.ReadFromJsonAsync<dynamic>();

            return View(resultados);
        }

        // Helper para obtener datos via AJAX si decides actualizar el gráfico sin recargar la página
        [HttpGet]
        public async Task<IActionResult> ObtenerJson(string? provincia, string? canton, string? parroquia, int? eleccionId)
        {
            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");
            string url = string.IsNullOrWhiteSpace(provincia)
                ? $"api/Resultados/Generales?eleccionId={eleccionId}"
                : $"api/Resultados/PorDireccion?provincia={provincia}&canton={canton}&parroquia={parroquia}&eleccionId={eleccionId}";

            var response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                return Content(data, "application/json");
            }
            return BadRequest();
        }
    }
}