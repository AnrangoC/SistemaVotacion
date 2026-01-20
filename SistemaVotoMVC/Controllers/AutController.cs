using Microsoft.AspNetCore.Mvc;
using SistemaVotoMVC.Models;
using SistemaVotoMVC.DTOs;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace SistemaVotoMVC.Controllers
{
    public class AutController : Controller
    {
        // Solo debe existir UNA declaración de esta variable
        private readonly IHttpClientFactory _httpClientFactory;

        public AutController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ActionName("Login")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LoginPost(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");

            // Se envía el objeto a la API como JSON
            var response = await client.PostAsJsonAsync("api/Aut/LoginGestion", new
            {
                Cedula = model.Cedula,
                Password = model.Password
            });

            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError(string.Empty, "Credenciales incorrectas.");
                return View(model);
            }

            var usuario = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
            if (usuario == null) return View(model);

            // Configuración de la sesión (Cookies)
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, usuario.NombreCompleto),
                new Claim(ClaimTypes.NameIdentifier, usuario.Cedula),
                new Claim(ClaimTypes.Role, usuario.RolId.ToString())
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity));

            // Redirección por Rol
            if (usuario.RolId == 1) return RedirectToAction("Index", "Admin");
            if (usuario.RolId == 3) return RedirectToAction("Index", "JefeJunta");

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}