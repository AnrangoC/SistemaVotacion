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
        private readonly string _endpointJuntas = "api/Juntas";

        public AdminController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // PANEL PRINCIPAL
        [HttpGet]
        public IActionResult Main()
        {
            ViewBag.NombreAdmin = User.Identity?.Name ?? "Administrador";
            return View();
        }

        // VOTANTES
        [HttpGet]
        public async Task<IActionResult> GestionVotantes()
        {
            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");

            // 1) Votantes
            var response = await client.GetAsync(_endpointVotantes);

            var lista = response.IsSuccessStatusCode
                ? await response.Content.ReadFromJsonAsync<List<Votante>>() ?? new List<Votante>()
                : new List<Votante>();

            if (!response.IsSuccessStatusCode)
                TempData["Error"] = "No se pudo obtener la lista de votantes desde la API.";

            // 2) Juntas (para el combo)
            var respJuntas = await client.GetAsync("api/Juntas");
            if (respJuntas.IsSuccessStatusCode)
            {
                // Usamos el DTO que ya tenemos porque trae Ubicacion y DireccionId correctos
                var juntas = await respJuntas.Content.ReadFromJsonAsync<List<SistemaVotoModelos.DTOs.JuntaDetalleDto>>()
                            ?? new List<SistemaVotoModelos.DTOs.JuntaDetalleDto>();

                ViewBag.Juntas = juntas;
            }
            else
            {
                ViewBag.Juntas = new List<SistemaVotoModelos.DTOs.JuntaDetalleDto>();
                TempData["ErrorJuntas"] = "No se pudo obtener juntas desde la API.";
            }

            return View(lista);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearVotante(Votante v)
        {
            if (v == null)
            {
                TempData["Error"] = "Datos inválidos.";
                return RedirectToAction(nameof(GestionVotantes));
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
                return RedirectToAction(nameof(GestionVotantes));
            }

            var apiMsg = await response.Content.ReadAsStringAsync();
            TempData["Error"] = string.IsNullOrWhiteSpace(apiMsg) ? "No se pudo crear el usuario." : apiMsg;

            return RedirectToAction(nameof(GestionVotantes));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarVotante(Votante v)
        {
            if (v == null || string.IsNullOrWhiteSpace(v.Cedula))
            {
                TempData["Error"] = "Datos inválidos.";
                return RedirectToAction(nameof(GestionVotantes));
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
                TempData["Error"] = string.IsNullOrWhiteSpace(apiMsg)
                    ? "Error al intentar actualizar el usuario."
                    : apiMsg;
            }

            return RedirectToAction(nameof(GestionVotantes));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarVotante(string cedula)
        {
            if (string.IsNullOrWhiteSpace(cedula))
            {
                TempData["Error"] = "Cédula inválida.";
                return RedirectToAction(nameof(GestionVotantes));
            }

            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");
            var response = await client.DeleteAsync($"{_endpointVotantes}/{cedula.Trim()}");

            if (response.IsSuccessStatusCode)
                TempData["Mensaje"] = "Usuario eliminado.";
            else
            {
                var apiMsg = await response.Content.ReadAsStringAsync();
                TempData["Error"] = string.IsNullOrWhiteSpace(apiMsg)
                    ? "No se pudo eliminar el registro."
                    : apiMsg;
            }

            return RedirectToAction(nameof(GestionVotantes));
        }

        // ELECCIONES
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
                return RedirectToAction(nameof(GestionElecciones));
            }

            e.Titulo = e.Titulo.Trim();
            e.Estado = "";

            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");
            var response = await client.PostAsJsonAsync(_endpointElecciones, e);

            if (response.IsSuccessStatusCode)
                TempData["Mensaje"] = "Elección creada correctamente.";
            else
            {
                var apiMsg = await response.Content.ReadAsStringAsync();
                TempData["Error"] = string.IsNullOrWhiteSpace(apiMsg) ? "No se pudo crear la elección." : apiMsg;
            }

            return RedirectToAction(nameof(GestionElecciones));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarEleccion(Eleccion e)
        {
            if (e == null || e.Id <= 0)
            {
                TempData["Error"] = "Datos inválidos.";
                return RedirectToAction(nameof(GestionElecciones));
            }

            e.Titulo = (e.Titulo ?? "").Trim();
            e.Estado = "";

            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");
            var response = await client.PutAsJsonAsync($"{_endpointElecciones}/{e.Id}", e);

            if (response.IsSuccessStatusCode)
                TempData["Mensaje"] = "Elección actualizada.";
            else
            {
                var apiMsg = await response.Content.ReadAsStringAsync();
                TempData["Error"] = string.IsNullOrWhiteSpace(apiMsg) ? "No se pudo actualizar la elección." : apiMsg;
            }

            return RedirectToAction(nameof(GestionElecciones));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarEleccion(int id)
        {
            if (id <= 0)
            {
                TempData["Error"] = "Id inválido.";
                return RedirectToAction(nameof(GestionElecciones));
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

            return RedirectToAction(nameof(GestionElecciones));
        }

        // LISTAS
        [HttpGet]
        public async Task<IActionResult> GestionListas(int eleccionId)
        {
            if (eleccionId <= 0)
            {
                TempData["Error"] = "Elección inválida.";
                return RedirectToAction(nameof(GestionElecciones));
            }

            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");

            var respEleccion = await client.GetAsync($"{_endpointElecciones}/{eleccionId}");
            if (!respEleccion.IsSuccessStatusCode)
            {
                TempData["Error"] = "Elección no encontrada.";
                return RedirectToAction(nameof(GestionElecciones));
            }

            ViewBag.Eleccion = await respEleccion.Content.ReadFromJsonAsync<Eleccion>();

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
                return RedirectToAction(nameof(GestionElecciones));
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

            return RedirectToAction(nameof(GestionListas), new { eleccionId = l.EleccionId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarLista(Lista l)
        {
            if (l == null || l.Id <= 0 || l.EleccionId <= 0)
            {
                TempData["Error"] = "Datos inválidos.";
                return RedirectToAction(nameof(GestionElecciones));
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

            return RedirectToAction(nameof(GestionListas), new { eleccionId = l.EleccionId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarLista(int id, int eleccionId)
        {
            if (id <= 0 || eleccionId <= 0)
            {
                TempData["Error"] = "Datos inválidos.";
                return RedirectToAction(nameof(GestionElecciones));
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

            return RedirectToAction(nameof(GestionListas), new { eleccionId });
        }

        // CANDIDATOS
        [HttpGet]
        public async Task<IActionResult> GestionCandidatos(int eleccionId)
        {
            if (eleccionId <= 0)
            {
                TempData["Error"] = "Elección inválida.";
                return RedirectToAction(nameof(GestionElecciones));
            }

            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");

            var respEleccion = await client.GetAsync($"{_endpointElecciones}/{eleccionId}");
            if (!respEleccion.IsSuccessStatusCode)
            {
                TempData["Error"] = "Elección no encontrada.";
                return RedirectToAction(nameof(GestionElecciones));
            }

            ViewBag.Eleccion = await respEleccion.Content.ReadFromJsonAsync<Eleccion>();

            var respListas = await client.GetAsync($"{_endpointListas}/PorEleccion/{eleccionId}");
            var listas = respListas.IsSuccessStatusCode
                ? await respListas.Content.ReadFromJsonAsync<List<Lista>>() ?? new List<Lista>()
                : new List<Lista>();

            ViewBag.Listas = listas;

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
            if (c == null || c.EleccionId <= 0 || c.ListaId <= 0 ||
                string.IsNullOrWhiteSpace(c.Cedula) || string.IsNullOrWhiteSpace(c.RolPostulante))
            {
                TempData["Error"] = "Datos inválidos.";
                return RedirectToAction(nameof(GestionElecciones));
            }

            c.Cedula = c.Cedula.Trim();
            c.RolPostulante = c.RolPostulante.Trim();

            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");
            var response = await client.PostAsJsonAsync(_endpointCandidatos, c);

            if (response.IsSuccessStatusCode)
                TempData["Mensaje"] = "Candidato registrado.";
            else
            {
                var apiMsg = await response.Content.ReadAsStringAsync();
                TempData["Error"] = string.IsNullOrWhiteSpace(apiMsg) ? "No se pudo crear el candidato." : apiMsg;
            }

            return RedirectToAction(nameof(GestionCandidatos), new { eleccionId = c.EleccionId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarCandidato(int id, int eleccionId, int listaId, string rolPostulante)
        {
            if (id <= 0 || eleccionId <= 0 || listaId <= 0 || string.IsNullOrWhiteSpace(rolPostulante))
            {
                TempData["Error"] = "Datos inválidos.";
                return RedirectToAction(nameof(GestionCandidatos), new { eleccionId });
            }

            var cambios = new Candidato
            {
                Id = id,
                EleccionId = eleccionId,
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

            return RedirectToAction(nameof(GestionCandidatos), new { eleccionId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarCandidato(int id, int eleccionId)
        {
            if (id <= 0 || eleccionId <= 0)
            {
                TempData["Error"] = "Datos inválidos.";
                return RedirectToAction(nameof(GestionCandidatos), new { eleccionId });
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

            return RedirectToAction(nameof(GestionCandidatos), new { eleccionId });
        }
    }
}
