using Microsoft.AspNetCore.Mvc;
using SistemaVotoMVC.DTOs;
using SistemaVotoMVC.Models;
using System.Net.Http.Json;

public class CuentasController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;

    public CuentasController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet]
    public IActionResult Ingreso()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Ingreso(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var client = _httpClientFactory.CreateClient("SistemaVotoAPI");

        var response = await client.PostAsJsonAsync(
            "api/Aut/LoginGestion",
            new
            {
                cedula = model.Cedula,
                password = model.Password
            });

        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError("", "Credenciales incorrectas");
            return View(model);
        }

        var usuario = await response.Content.ReadFromJsonAsync<LoginResponseDto>();

        // Por ahora solo redirigimos según rol
        if (usuario!.RolId == 1)
            return RedirectToAction("Index", "Admin");

        if (usuario.RolId == 3)
            return RedirectToAction("Index", "Junta");

        return RedirectToAction("Index", "Home");
    }
}
