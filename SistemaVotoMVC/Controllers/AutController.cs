using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using SistemaVotoModelos.DTOs;
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
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(LoginRequestDto loginRequest)
        {
            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");
            var response = await client.PostAsJsonAsync("api/Aut/LoginGestion", loginRequest);

            if (response.IsSuccessStatusCode)
            {
                var usuario = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, usuario.NombreCompleto),
                    new Claim(ClaimTypes.NameIdentifier, usuario.Cedula),
                    new Claim(ClaimTypes.Role, usuario.RolId.ToString())
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                return RedirectToAction("Main", "Admin");
            }

            ViewBag.Error = "Credenciales incorrectas.";
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Aut");
        }
    }
}