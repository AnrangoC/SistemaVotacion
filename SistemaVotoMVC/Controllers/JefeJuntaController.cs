using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaVotoModelos;
using SistemaVotoModelos.DTOs;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;

namespace SistemaVotoMVC.Controllers
{
    [Authorize(Roles = "3")]
    public class JefeJuntaController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public JefeJuntaController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private string GetCedulaJefe()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
        }

        private async Task<long> GetJuntaIdActualAsync()
        {
            var cedulaJefe = GetCedulaJefe();
            if (string.IsNullOrWhiteSpace(cedulaJefe))
                return 0;

            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");

            var resp = await client.GetAsync($"api/Juntas/DeJefeActual/{cedulaJefe}");
            if (!resp.IsSuccessStatusCode)
                return 0;

            var raw = await resp.Content.ReadAsStringAsync();

            try
            {
                using var doc = JsonDocument.Parse(raw);
                if (doc.RootElement.TryGetProperty("id", out var pid))
                    return pid.GetInt64();
                if (doc.RootElement.TryGetProperty("Id", out var pId))
                    return pId.GetInt64();
            }
            catch { }

            try
            {
                var junta = await resp.Content.ReadFromJsonAsync<Junta>();
                return junta?.Id ?? 0;
            }
            catch { }

            return 0;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            ViewBag.NombreJefe = User.Identity?.Name ?? "Jefe de Junta";

            var juntaId = await GetJuntaIdActualAsync();
            ViewBag.JuntaId = juntaId;

            if (juntaId <= 0)
            {
                TempData["Error"] = "No tienes una junta asignada para la elección activa.";
                return View(new List<Votante>());
            }

            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");

            await client.GetAsync("api/Elecciones");

            var resp = await client.GetAsync($"api/Votantes/PorJunta/{juntaId}");

            var votantes = resp.IsSuccessStatusCode
                ? await resp.Content.ReadFromJsonAsync<List<Votante>>() ?? new List<Votante>()
                : new List<Votante>();

            if (!resp.IsSuccessStatusCode)
                TempData["Error"] = "No se pudo obtener los votantes de tu junta.";

            return View(votantes);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerarToken(string cedulaVotante)
        {
            cedulaVotante = (cedulaVotante ?? "").Trim();
            if (string.IsNullOrWhiteSpace(cedulaVotante))
            {
                TempData["Error"] = "Cédula inválida.";
                return RedirectToAction(nameof(Index));
            }

            var cedulaJefe = GetCedulaJefe();
            if (string.IsNullOrWhiteSpace(cedulaJefe))
            {
                TempData["Error"] = "No se pudo validar tu sesión como jefe de junta.";
                return RedirectToAction(nameof(Index));
            }

            var juntaId = await GetJuntaIdActualAsync();
            if (juntaId <= 0)
            {
                TempData["Error"] = "No tienes una junta asignada para la elección activa.";
                return RedirectToAction(nameof(Index));
            }

            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");

            var resp = await client.PostAsync(
                $"api/Aut/GenerarToken?cedulaJefe={cedulaJefe}&cedulaVotante={cedulaVotante}",
                null
            );

            var raw = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                TempData["Error"] = string.IsNullOrWhiteSpace(raw) ? "No se pudo generar el token." : raw;
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var dto = await resp.Content.ReadFromJsonAsync<TokenVotacionDto>();
                if (dto != null && !string.IsNullOrWhiteSpace(dto.CodigoToken))
                {
                    TempData["TokenOk"] = $"Token para {dto.CedulaVotante}: {dto.CodigoToken}";
                    TempData["TokenExp"] = dto.Expiracion.ToString("yyyy-MM-dd HH:mm");
                    return RedirectToAction(nameof(Index));
                }
            }
            catch { }

            try
            {
                using var doc = JsonDocument.Parse(raw);
                var root = doc.RootElement;

                string? ced = null;
                string? tok = null;
                string? exp = null;

                if (root.TryGetProperty("Cedula", out var pCed)) ced = pCed.GetString();
                if (root.TryGetProperty("cedula", out var pced)) ced ??= pced.GetString();

                if (root.TryGetProperty("Token", out var pTok)) tok = pTok.GetString();
                if (root.TryGetProperty("token", out var ptok)) tok ??= ptok.GetString();

                if (root.TryGetProperty("Expira", out var pExp)) exp = pExp.GetString();
                if (root.TryGetProperty("expira", out var pexp)) exp ??= pexp.GetString();

                if (root.TryGetProperty("codigo", out var pCod)) tok ??= pCod.GetString();
                if (root.TryGetProperty("Codigo", out var pCodigo)) tok ??= pCodigo.GetString();

                if (!string.IsNullOrWhiteSpace(tok))
                {
                    TempData["TokenOk"] = $"Token para {(ced ?? cedulaVotante)}: {tok}";
                    if (!string.IsNullOrWhiteSpace(exp))
                        TempData["TokenExp"] = exp;
                    return RedirectToAction(nameof(Index));
                }
            }
            catch { }

            TempData["Error"] = "No se pudo leer el token generado.";
            return RedirectToAction(nameof(Index));
        }
    }
}
