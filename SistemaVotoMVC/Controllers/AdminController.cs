using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaVotoModelos;
using System.Net.Http.Json;

namespace SistemaVotoMVC.Controllers
{
    [Authorize(Roles = "1")]
    public class AdminController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _endpoint = "api/Votantes";

        public AdminController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public IActionResult Main()
        {
            ViewBag.NombreAdmin = User.Identity.Name;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GestionVotantes()
        {
            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");
            var response = await client.GetAsync(_endpoint);
            var lista = response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<Votante>>() : new List<Votante>();
            return View(lista);
        }

        [HttpPost]
        public async Task<IActionResult> CrearVotante(Votante v)
        {
            v.Estado = true;
            v.HaVotado = false;
            // Si es Rol 2, enviamos password vacío para que la API asigne una contraseña vacía que ya controlamos en las vistas por rol
            if (v.RolId == 2) v.Password = "";

            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");
            var response = await client.PostAsJsonAsync(_endpoint, v);

            if (response.IsSuccessStatusCode) TempData["Mensaje"] = "Usuario creado exitosamente.";
            else TempData["Error"] = "No se pudo crear el usuario.";

            return RedirectToAction("GestionVotantes");
        }

        [HttpPost]
        public async Task<IActionResult> EditarVotante(Votante v)
        {
            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");
            // Invocamos el método PUT de tu API usando la cédula como identificador
            var response = await client.PutAsJsonAsync($"{_endpoint}/{v.Cedula}", v);

            if (response.IsSuccessStatusCode) TempData["Mensaje"] = "Datos actualizados correctamente.";
            else TempData["Error"] = "Error al intentar actualizar el usuario.";

            return RedirectToAction("GestionVotantes");
        }

        [HttpPost]
        public async Task<IActionResult> EliminarVotante(string cedula)
        {
            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");
            var response = await client.DeleteAsync($"{_endpoint}/{cedula}");

            if (response.IsSuccessStatusCode) TempData["Mensaje"] = "Usuario eliminado.";
            else TempData["Error"] = "No se pudo eliminar el registro.";

            return RedirectToAction("GestionVotantes");
        }

    }
}