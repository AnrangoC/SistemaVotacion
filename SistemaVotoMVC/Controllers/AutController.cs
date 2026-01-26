using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using SistemaVotoModelos.DTOs;
using SistemaVotoMVC.DTOs;
using SistemaVotoMVC.Models;
using System.Net.Http.Json;
using System.Security.Claims;

namespace SistemaVotoMVC.Controllers
{
    public class AutController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public AutController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View(new LoginViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");

            var response = await client.PostAsJsonAsync("api/Aut/LoginGestion", new LoginRequestDto
            {
                Cedula = model.Cedula,
                Password = model.Password
            });

            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", "Cédula o contraseña incorrecta.");
                return View(model);
            }

            var usuario = await response.Content.ReadFromJsonAsync<LoginResponseDto>();

            if (usuario == null)
            {
                ModelState.AddModelError("", "No se pudo leer la respuesta del servidor.");
                return View(model);
            }

            // Solo admin (1) y jefe de junta (3) deberían llegar aquí, pero igual queda seguro
            if (usuario.RolId != 1 && usuario.RolId != 3)
            {
                ModelState.AddModelError("", "No tienes permisos para ingresar al panel de gestión.");
                return View(model);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Cedula),
                new Claim(ClaimTypes.Name, usuario.NombreCompleto),
                new Claim(ClaimTypes.Role, usuario.RolId.ToString())
            };

            if (usuario.RolId == 3)
                claims.Add(new Claim("JuntaId", (usuario.JuntaId ?? 0).ToString()));

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity)
            );

            return usuario.RolId == 1
                ? RedirectToAction("Main", "Admin")
                : RedirectToAction("Index", "Junta");
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}
