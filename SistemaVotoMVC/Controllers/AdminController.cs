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

        private readonly string _endpointVotantes = "api/Votantes";
        private readonly string _endpointElecciones = "api/Elecciones";
        private readonly string _endpointListas = "api/Listas";
        private readonly string _endpointCandidatos = "api/Candidatos";

        public AdminController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public IActionResult Main()
        {
            ViewBag.NombreAdmin = User.Identity?.Name ?? "Administrador";
            return View();
        }

        // =========================
        // VOTANTES (ya lo tenías)
        // =========================
        [HttpGet]
        public async Task<IActionResult> GestionVotantes()
        {
            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");
            var response = await client.GetAsync(_endpointVotantes);

            var lista = response.IsSuccessStatusCode
                ? await response.Content.ReadFromJsonAsync<List<Votante>>() ?? new List<Votante>()
                : new List<Votante>();

            if (!response.IsSuccessStatusCode)
                TempData["Error"] = "No se pudo obtener la lista de votantes desde la API.";

            return View(lista);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearVotante(Votante v)
        {
            if (v == null)
            {
                TempData["Error"] = "Datos inválidos.";
                return RedirectToAction("GestionVotantes");
            }

            v.Cedula = (v.Cedula ?? "").Trim();
            v.NombreCompleto = (v.NombreCompleto ?? "").Trim();
            v.Email = (v.Email ?? "").Trim();
            v.FotoUrl = (v.FotoUrl ?? "").Trim();

            if (v.RolId == 2)
                v.Password = "";

            if (v.JuntaId.HasValue && v.JuntaId.Value <= 0)
                v.JuntaId = null;

            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");
            var response = await client.PostAsJsonAsync(_endpointVotantes, v);

            if (response.IsSuccessStatusCode)
            {
                TempData["Mensaje"] = "Usuario creado exitosamente.";
                return RedirectToAction("GestionVotantes");
            }

            var apiMsg = await response.Content.ReadAsStringAsync();
            TempData["Error"] = string.IsNullOrWhiteSpace(apiMsg) ? "No se pudo crear el usuario." : apiMsg;

            return RedirectToAction("GestionVotantes");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarVotante(Votante v)
        {
            if (v == null || string.IsNullOrWhiteSpace(v.Cedula))
            {
                TempData["Error"] = "Datos inválidos.";
                return RedirectToAction("GestionVotantes");
            }

            v.Cedula = v.Cedula.Trim();
            v.NombreCompleto = (v.NombreCompleto ?? "").Trim();
            v.Email = (v.Email ?? "").Trim();
            v.FotoUrl = (v.FotoUrl ?? "").Trim();

            if (v.RolId == 2 && string.IsNullOrWhiteSpace(v.Password))
                v.Password = "";

            if (v.JuntaId.HasValue && v.JuntaId.Value <= 0)
                v.JuntaId = null;

            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");
            var response = await client.PutAsJsonAsync($"{_endpointVotantes}/{v.Cedula}", v);

            if (response.IsSuccessStatusCode)
                TempData["Mensaje"] = "Datos actualizados correctamente.";
            else
            {
                var apiMsg = await response.Content.ReadAsStringAsync();
                TempData["Error"] = string.IsNullOrWhiteSpace(apiMsg) ? "Error al intentar actualizar el usuario." : apiMsg;
            }

            return RedirectToAction("GestionVotantes");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarVotante(string cedula)
        {
            if (string.IsNullOrWhiteSpace(cedula))
            {
                TempData["Error"] = "Cédula inválida.";
                return RedirectToAction("GestionVotantes");
            }

            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");
            var response = await client.DeleteAsync($"{_endpointVotantes}/{cedula.Trim()}");

            if (response.IsSuccessStatusCode)
                TempData["Mensaje"] = "Usuario eliminado.";
            else
            {
                var apiMsg = await response.Content.ReadAsStringAsync();
                TempData["Error"] = string.IsNullOrWhiteSpace(apiMsg) ? "No se pudo eliminar el registro." : apiMsg;
            }

            return RedirectToAction("GestionVotantes");
        }

        // =========================
        // ELECCIONES
        // =========================
        [HttpGet]
        public async Task<IActionResult> GestionElecciones()
        {
            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");
            var response = await client.GetAsync(_endpointElecciones);

            var lista = response.IsSuccessStatusCode
                ? await response.Content.ReadFromJsonAsync<List<Eleccion>>() ?? new List<Eleccion>()
                : new List<Eleccion>();

            if (!response.IsSuccessStatusCode)
                TempData["Error"] = "No se pudo obtener la lista de elecciones desde la API.";

            return View(lista);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearEleccion(Eleccion e)
        {
            if (e == null || string.IsNullOrWhiteSpace(e.Titulo))
            {
                TempData["Error"] = "Datos inválidos.";
                return RedirectToAction("GestionElecciones");
            }

            e.Titulo = e.Titulo.Trim();
            if (string.IsNullOrWhiteSpace(e.Estado))
                e.Estado = "CONFIGURACION";

            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");
            var response = await client.PostAsJsonAsync(_endpointElecciones, e);

            if (response.IsSuccessStatusCode)
                TempData["Mensaje"] = "Elección creada correctamente.";
            else
            {
                var apiMsg = await response.Content.ReadAsStringAsync();
                TempData["Error"] = string.IsNullOrWhiteSpace(apiMsg) ? "No se pudo crear la elección." : apiMsg;
            }

            return RedirectToAction("GestionElecciones");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarEleccion(Eleccion e)
        {
            if (e == null || e.Id <= 0)
            {
                TempData["Error"] = "Datos inválidos.";
                return RedirectToAction("GestionElecciones");
            }

            e.Titulo = (e.Titulo ?? "").Trim();
            e.Estado = (e.Estado ?? "CONFIGURACION").Trim();

            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");
            var response = await client.PutAsJsonAsync($"{_endpointElecciones}/{e.Id}", e);

            if (response.IsSuccessStatusCode)
                TempData["Mensaje"] = "Elección actualizada.";
            else
            {
                var apiMsg = await response.Content.ReadAsStringAsync();
                TempData["Error"] = string.IsNullOrWhiteSpace(apiMsg) ? "No se pudo actualizar la elección." : apiMsg;
            }

            return RedirectToAction("GestionElecciones");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarEleccion(int id)
        {
            if (id <= 0)
            {
                TempData["Error"] = "Id inválido.";
                return RedirectToAction("GestionElecciones");
            }

            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");
            var response = await client.DeleteAsync($"{_endpointElecciones}/{id}");

            if (response.IsSuccessStatusCode)
                TempData["Mensaje"] = "Elección eliminada.";
            else
            {
                var apiMsg = await response.Content.ReadAsStringAsync();
                TempData["Error"] = string.IsNullOrWhiteSpace(apiMsg) ? "No se pudo eliminar la elección." : apiMsg;
            }

            return RedirectToAction("GestionElecciones");
        }

        // =========================
        // LISTAS (por elección)
        // =========================
        [HttpGet]
        public async Task<IActionResult> GestionListas(int eleccionId)
        {
            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");

            var respEleccion = await client.GetAsync($"{_endpointElecciones}/{eleccionId}");
            if (!respEleccion.IsSuccessStatusCode)
            {
                TempData["Error"] = "Elección no encontrada.";
                return RedirectToAction("GestionElecciones");
            }

            var eleccion = await respEleccion.Content.ReadFromJsonAsync<Eleccion>();
            ViewBag.Eleccion = eleccion;

            var respListas = await client.GetAsync($"{_endpointListas}/PorEleccion/{eleccionId}");
            var listas = respListas.IsSuccessStatusCode
                ? await respListas.Content.ReadFromJsonAsync<List<Lista>>() ?? new List<Lista>()
                : new List<Lista>();

            if (!respListas.IsSuccessStatusCode)
                TempData["Error"] = "No se pudo obtener las listas de la elección.";

            return View(listas);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearLista(Lista l)
        {
            if (l == null || l.EleccionId <= 0 || string.IsNullOrWhiteSpace(l.NombreLista))
            {
                TempData["Error"] = "Datos inválidos.";
                return RedirectToAction("GestionElecciones");
            }

            l.NombreLista = l.NombreLista.Trim();
            l.LogoUrl = (l.LogoUrl ?? "").Trim();

            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");
            var response = await client.PostAsJsonAsync(_endpointListas, l);

            if (response.IsSuccessStatusCode)
                TempData["Mensaje"] = "Lista creada.";
            else
            {
                var apiMsg = await response.Content.ReadAsStringAsync();
                TempData["Error"] = string.IsNullOrWhiteSpace(apiMsg) ? "No se pudo crear la lista." : apiMsg;
            }

            return RedirectToAction("GestionListas", new { eleccionId = l.EleccionId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarLista(Lista l)
        {
            if (l == null || l.Id <= 0 || l.EleccionId <= 0)
            {
                TempData["Error"] = "Datos inválidos.";
                return RedirectToAction("GestionElecciones");
            }

            l.NombreLista = (l.NombreLista ?? "").Trim();
            l.LogoUrl = (l.LogoUrl ?? "").Trim();

            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");
            var response = await client.PutAsJsonAsync($"{_endpointListas}/{l.Id}", l);

            if (response.IsSuccessStatusCode)
                TempData["Mensaje"] = "Lista actualizada.";
            else
            {
                var apiMsg = await response.Content.ReadAsStringAsync();
                TempData["Error"] = string.IsNullOrWhiteSpace(apiMsg) ? "No se pudo actualizar la lista." : apiMsg;
            }

            return RedirectToAction("GestionListas", new { eleccionId = l.EleccionId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarLista(int id, int eleccionId)
        {
            if (id <= 0 || eleccionId <= 0)
            {
                TempData["Error"] = "Datos inválidos.";
                return RedirectToAction("GestionElecciones");
            }

            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");
            var response = await client.DeleteAsync($"{_endpointListas}/{id}");

            if (response.IsSuccessStatusCode)
                TempData["Mensaje"] = "Lista eliminada.";
            else
            {
                var apiMsg = await response.Content.ReadAsStringAsync();
                TempData["Error"] = string.IsNullOrWhiteSpace(apiMsg) ? "No se pudo eliminar la lista." : apiMsg;
            }

            return RedirectToAction("GestionListas", new { eleccionId });
        }

        // =========================
        // CANDIDATOS (por elección)
        // =========================
        [HttpGet]
        public async Task<IActionResult> GestionCandidatos(int eleccionId)
        {
            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");

            var respEleccion = await client.GetAsync($"{_endpointElecciones}/{eleccionId}");
            if (!respEleccion.IsSuccessStatusCode)
            {
                TempData["Error"] = "Elección no encontrada.";
                return RedirectToAction("GestionElecciones");
            }

            var eleccion = await respEleccion.Content.ReadFromJsonAsync<Eleccion>();
            ViewBag.Eleccion = eleccion;

            // listas para el dropdown
            var respListas = await client.GetAsync($"{_endpointListas}/PorEleccion/{eleccionId}");
            var listas = respListas.IsSuccessStatusCode
                ? await respListas.Content.ReadFromJsonAsync<List<Lista>>() ?? new List<Lista>()
                : new List<Lista>();

            ViewBag.Listas = listas;

            // candidatos de esa elección
            var respCand = await client.GetAsync($"{_endpointCandidatos}/PorEleccion/{eleccionId}");
            var candidatos = respCand.IsSuccessStatusCode
                ? await respCand.Content.ReadFromJsonAsync<List<Candidato>>() ?? new List<Candidato>()
                : new List<Candidato>();

            if (!respCand.IsSuccessStatusCode)
                TempData["Error"] = "No se pudo obtener los candidatos de la elección.";

            return View(candidatos);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearCandidato(Candidato c)
        {
            if (c == null || c.EleccionId <= 0 || c.ListaId <= 0 || string.IsNullOrWhiteSpace(c.Cedula) || string.IsNullOrWhiteSpace(c.RolPostulante))
            {
                TempData["Error"] = "Datos inválidos.";
                return RedirectToAction("GestionElecciones");
            }

            c.Cedula = c.Cedula.Trim();
            c.RolPostulante = c.RolPostulante.Trim();

            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");
            var response = await client.PostAsJsonAsync($"{_endpointCandidatos}", c);

            if (response.IsSuccessStatusCode)
                TempData["Mensaje"] = "Candidato registrado.";
            else
            {
                var apiMsg = await response.Content.ReadAsStringAsync();
                TempData["Error"] = string.IsNullOrWhiteSpace(apiMsg) ? "No se pudo crear el candidato." : apiMsg;
            }

            return RedirectToAction("GestionCandidatos", new { eleccionId = c.EleccionId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarCandidato(int id, int eleccionId, int listaId, string rolPostulante)
        {
            if (id <= 0 || eleccionId <= 0 || listaId <= 0 || string.IsNullOrWhiteSpace(rolPostulante))
            {
                TempData["Error"] = "Datos inválidos.";
                return RedirectToAction("GestionCandidatos", new { eleccionId });
            }

            var cambios = new Candidato
            {
                Id = id,
                EleccionId = eleccionId,   // la API no lo edita, pero lo mando por consistencia
                ListaId = listaId,
                RolPostulante = rolPostulante.Trim()
            };

            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");
            var response = await client.PutAsJsonAsync($"{_endpointCandidatos}/{id}", cambios);

            if (response.IsSuccessStatusCode)
                TempData["Mensaje"] = "Candidato actualizado.";
            else
            {
                var apiMsg = await response.Content.ReadAsStringAsync();
                TempData["Error"] = string.IsNullOrWhiteSpace(apiMsg) ? "No se pudo actualizar el candidato." : apiMsg;
            }

            return RedirectToAction("GestionCandidatos", new { eleccionId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarCandidato(int id, int eleccionId)
        {
            if (id <= 0 || eleccionId <= 0)
            {
                TempData["Error"] = "Datos inválidos.";
                return RedirectToAction("GestionCandidatos", new { eleccionId });
            }

            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");
            var response = await client.DeleteAsync($"{_endpointCandidatos}/{id}");

            if (response.IsSuccessStatusCode)
                TempData["Mensaje"] = "Candidato eliminado.";
            else
            {
                var apiMsg = await response.Content.ReadAsStringAsync();
                TempData["Error"] = string.IsNullOrWhiteSpace(apiMsg) ? "No se pudo eliminar el candidato." : apiMsg;
            }

            return RedirectToAction("GestionCandidatos", new { eleccionId });
        }

    }
}
