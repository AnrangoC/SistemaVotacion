using Microsoft.AspNetCore.Mvc;
using SistemaVotoMVC.Models;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net.Http.Json;

namespace SistemaVotoMVC.Controllers
{
    public class AutController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public AutController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // GET: /Aut/Login
        public IActionResult Login()
        {
            return View();
        }

        // POST: /Aut/Login
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");

            var response = await client.PostAsync(
                $"api/Aut/LoginGestion?cedula={model.Cedula}&password={model.Password}",
                null
            );

            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", "Credenciales incorrectas");
                return View(model);
            }

            var usuario = await response.Content.ReadFromJsonAsync<dynamic>();

            // Por ahora solo redirigimos según rol
            int rol = usuario.rolId;

            if (rol == 1)
                return RedirectToAction("Index", "Admin");

            if (rol == 3)
                return RedirectToAction("Index", "JefeJunta");

            return RedirectToAction("Index", "Home");
        }
    }
}
