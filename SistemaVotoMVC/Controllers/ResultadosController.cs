using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Threading.Tasks;

namespace SistemaVotoMVC.Controllers
{
    public class ResultadosController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ResultadosController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // Esta es la acción que abre el botón de tu Home
        public IActionResult Index()
        {
            return View();
        }

        // Este método ayuda a la vista a obtener los datos de la API sin recargar
        [HttpGet]
        public async Task<IActionResult> ObtenerDatosFiltrados(string provincia, string? canton, string? parroquia)
        {
            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");
            var response = await client.GetAsync($"api/Resultados/PorDireccion?provincia={provincia}&canton={canton}&parroquia={parroquia}");

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                return Content(data, "application/json");
            }
            return BadRequest();
        }
    }
}