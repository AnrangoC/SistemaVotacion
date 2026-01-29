using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaVotoModelos;
using SistemaVotoModelos.DTOs;
using System.Net.Http.Json;

namespace SistemaVotoMVC.Controllers
{
    [Authorize(Roles = "1")]
    [Route("[controller]/[action]")]
    public class AdminController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        private readonly string _endpointVotantes = "api/Votantes";
        private readonly string _endpointElecciones = "api/Elecciones";
        private readonly string _endpointListas = "api/Listas";
        private readonly string _endpointCandidatos = "api/Candidatos";
        private readonly string _endpointJuntas = "api/Juntas";
        private readonly string _endpointDirecciones = "api/Direcciones";

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

        [HttpGet]
        public async Task<IActionResult> GestionJuntas()
        {
            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");

            var response = await client.GetAsync(_endpointJuntas);
            var juntas = response.IsSuccessStatusCode
                ? await response.Content.ReadFromJsonAsync<List<JuntaDetalleDto>>() ?? new List<JuntaDetalleDto>()
                : new List<JuntaDetalleDto>();

            var respDir = await client.GetAsync(_endpointDirecciones);
            ViewBag.Direcciones = respDir.IsSuccessStatusCode
                ? await respDir.Content.ReadFromJsonAsync<List<Direccion>>() ?? new List<Direccion>()
                : new List<Direccion>();

            var respElec = await client.GetAsync(_endpointElecciones);
            ViewBag.Elecciones = respElec.IsSuccessStatusCode
                ? await respElec.Content.ReadFromJsonAsync<List<Eleccion>>() ?? new List<Eleccion>()
                : new List<Eleccion>();

            if (!response.IsSuccessStatusCode)
                TempData["Error"] = "No se pudo obtener el listado de juntas.";

            return View("~/Views/Juntas/Index.cshtml", juntas);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AprobarJunta(long id)
        {
            if (id <= 0)
            {
                TempData["Error"] = "Identificador de junta no válido.";
                return RedirectToAction(nameof(GestionJuntas));
            }

            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");
            var response = await client.PutAsync($"{_endpointJuntas}/AprobarJunta/{id}", null);

            if (response.IsSuccessStatusCode)
                TempData["Mensaje"] = "Junta aprobada exitosamente.";
            else
                TempData["Error"] = await response.Content.ReadAsStringAsync();

            return RedirectToAction(nameof(GestionJuntas));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearJuntaPorDireccion(int eleccionId, int direccionId, int cantidad)
        {
            if (eleccionId <= 0 || direccionId <= 0 || cantidad <= 0)
            {
                TempData["Error"] = "Datos para la creación de juntas inválidos.";
                return RedirectToAction(nameof(GestionJuntas));
            }

            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");
            var url = $"{_endpointJuntas}/CrearPorDireccion?eleccionId={eleccionId}&direccionId={direccionId}&cantidad={cantidad}";
            var response = await client.PostAsync(url, null);

            if (response.IsSuccessStatusCode)
                TempData["Mensaje"] = "Mesas creadas correctamente.";
            else
                TempData["Error"] = await response.Content.ReadAsStringAsync();

            return RedirectToAction(nameof(GestionJuntas));
        }

        [HttpGet]
        public async Task<IActionResult> GestionVotantes()
        {
            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");

            var respVotantes = await client.GetAsync(_endpointVotantes);
            var lista = respVotantes.IsSuccessStatusCode
                ? await respVotantes.Content.ReadFromJsonAsync<List<Votante>>() ?? new List<Votante>()
                : new List<Votante>();

            var respJuntas = await client.GetAsync(_endpointJuntas);
            ViewBag.Juntas = respJuntas.IsSuccessStatusCode
                ? await respJuntas.Content.ReadFromJsonAsync<List<JuntaDetalleDto>>() ?? new List<JuntaDetalleDto>()
                : new List<JuntaDetalleDto>();

            if (!respVotantes.IsSuccessStatusCode)
                TempData["Error"] = await respVotantes.Content.ReadAsStringAsync();

            return View(lista);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearVotante(Votante v)
        {
            if (v == null) return RedirectToAction(nameof(GestionVotantes));

            v.Cedula = (v.Cedula ?? "").Trim();
            v.NombreCompleto = (v.NombreCompleto ?? "").Trim();
            v.Email = (v.Email ?? "").Trim();
            v.FotoUrl = (v.FotoUrl ?? "").Trim();
            v.Password = (v.Password ?? "").Trim();

            if (v.RolId == 2 && string.IsNullOrWhiteSpace(v.Password))
                v.Password = "VOTANTE_NORMAL";

            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");
            var response = await client.PostAsJsonAsync(_endpointVotantes, v);

            if (response.IsSuccessStatusCode)
                TempData["Mensaje"] = "Usuario registrado correctamente.";
            else
                TempData["Error"] = await response.Content.ReadAsStringAsync();

            return RedirectToAction(nameof(GestionVotantes));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarVotante(Votante v)
        {
            if (v == null)
            {
                TempData["Error"] = "Datos no proporcionados.";
                return RedirectToAction(nameof(GestionVotantes));
            }

            v.Cedula = (v.Cedula ?? "").Trim();
            v.NombreCompleto = (v.NombreCompleto ?? "").Trim();
            v.Email = (v.Email ?? "").Trim();
            v.FotoUrl = (v.FotoUrl ?? "").Trim();

            // si viene vacío, la API NO cambia la contraseña
            v.Password = (v.Password ?? "").Trim();

            if (string.IsNullOrWhiteSpace(v.Cedula))
            {
                TempData["Error"] = "La cédula es obligatoria.";
                return RedirectToAction(nameof(GestionVotantes));
            }

            if (v.RolId < 1 || v.RolId > 3)
            {
                TempData["Error"] = "Rol inválido.";
                return RedirectToAction(nameof(GestionVotantes));
            }

            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");
            var resp = await client.PutAsJsonAsync($"{_endpointVotantes}/{v.Cedula}", v);

            if (resp.IsSuccessStatusCode)
                TempData["Mensaje"] = "Usuario actualizado.";
            else
                TempData["Error"] = await resp.Content.ReadAsStringAsync();

            return RedirectToAction(nameof(GestionVotantes));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarVotante(string cedula)
        {
            cedula = (cedula ?? "").Trim();
            if (string.IsNullOrWhiteSpace(cedula))
            {
                TempData["Error"] = "Cédula inválida.";
                return RedirectToAction(nameof(GestionVotantes));
            }

            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");
            var resp = await client.DeleteAsync($"{_endpointVotantes}/{cedula}");

            if (resp.IsSuccessStatusCode)
                TempData["Mensaje"] = "Usuario eliminado.";
            else
                TempData["Error"] = await resp.Content.ReadAsStringAsync();

            return RedirectToAction(nameof(GestionVotantes));
        }

        [HttpGet]
        public async Task<IActionResult> GestionElecciones()
        {
            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");
            var response = await client.GetAsync(_endpointElecciones);

            var lista = response.IsSuccessStatusCode
                ? await response.Content.ReadFromJsonAsync<List<Eleccion>>() ?? new List<Eleccion>()
                : new List<Eleccion>();

            if (!response.IsSuccessStatusCode)
                TempData["Error"] = await response.Content.ReadAsStringAsync();

            return View(lista);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearEleccion(Eleccion e)
        {
            if (e == null)
            {
                TempData["Error"] = "Datos no proporcionados.";
                return RedirectToAction(nameof(GestionElecciones));
            }

            e.Titulo = (e.Titulo ?? "").Trim();

            if (string.IsNullOrWhiteSpace(e.Titulo))
            {
                TempData["Error"] = "El título es obligatorio.";
                return RedirectToAction(nameof(GestionElecciones));
            }

            if (e.FechaFin <= e.FechaInicio)
            {
                TempData["Error"] = "La fecha/hora fin debe ser mayor que la fecha/hora inicio.";
                return RedirectToAction(nameof(GestionElecciones));
            }

            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");
            var response = await client.PostAsJsonAsync(_endpointElecciones, e);

            if (response.IsSuccessStatusCode)
                TempData["Mensaje"] = "Elección creada correctamente.";
            else
                TempData["Error"] = await response.Content.ReadAsStringAsync();

            return RedirectToAction(nameof(GestionElecciones));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarEleccion(Eleccion e)
        {
            if (e == null || e.Id <= 0) return RedirectToAction(nameof(GestionElecciones));

            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");
            var response = await client.PutAsJsonAsync($"{_endpointElecciones}/{e.Id}", e);

            if (response.IsSuccessStatusCode)
                TempData["Mensaje"] = "Elección actualizada.";
            else
                TempData["Error"] = await response.Content.ReadAsStringAsync();

            return RedirectToAction(nameof(GestionElecciones));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarEleccion(int id)
        {
            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");
            var response = await client.DeleteAsync($"{_endpointElecciones}/{id}");

            if (response.IsSuccessStatusCode)
                TempData["Mensaje"] = "Elección eliminada.";
            else
                TempData["Error"] = "No se puede eliminar una elección con datos asociados.";

            return RedirectToAction(nameof(GestionElecciones));
        }

        // LISTAS

        [HttpGet]
        public async Task<IActionResult> GestionListas(int eleccionId)
        {
            if (eleccionId <= 0)
            {
                TempData["Error"] = "Elección no válida.";
                return RedirectToAction(nameof(GestionElecciones));
            }

            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");

            var respElec = await client.GetAsync($"{_endpointElecciones}/{eleccionId}");
            ViewBag.Eleccion = respElec.IsSuccessStatusCode
                ? await respElec.Content.ReadFromJsonAsync<Eleccion>()
                : null;

            var resp = await client.GetAsync($"{_endpointListas}/PorEleccion/{eleccionId}");
            var listas = resp.IsSuccessStatusCode
                ? await resp.Content.ReadFromJsonAsync<List<Lista>>() ?? new List<Lista>()
                : new List<Lista>();

            if (!resp.IsSuccessStatusCode)
                TempData["Error"] = await resp.Content.ReadAsStringAsync();

            return View(listas);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearLista(Lista lista)
        {
            if (lista == null || lista.EleccionId <= 0)
            {
                TempData["Error"] = "No llegó la elección.";
                return RedirectToAction(nameof(GestionElecciones));
            }

            lista.NombreLista = (lista.NombreLista ?? "").Trim();
            lista.LogoUrl = (lista.LogoUrl ?? "").Trim();

            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");
            var resp = await client.PostAsJsonAsync(_endpointListas, lista);

            if (resp.IsSuccessStatusCode)
                TempData["Mensaje"] = "Lista creada.";
            else
                TempData["Error"] = await resp.Content.ReadAsStringAsync();

            return RedirectToAction(nameof(GestionListas), new { eleccionId = lista.EleccionId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarLista(Lista cambios)
        {
            if (cambios == null || cambios.Id <= 0 || cambios.EleccionId <= 0)
            {
                TempData["Error"] = "Datos inválidos.";
                return RedirectToAction(nameof(GestionElecciones));
            }

            cambios.NombreLista = (cambios.NombreLista ?? "").Trim();
            cambios.LogoUrl = (cambios.LogoUrl ?? "").Trim();

            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");
            var resp = await client.PutAsJsonAsync($"{_endpointListas}/{cambios.Id}", cambios);

            if (resp.IsSuccessStatusCode)
                TempData["Mensaje"] = "Lista actualizada.";
            else
                TempData["Error"] = await resp.Content.ReadAsStringAsync();

            return RedirectToAction(nameof(GestionListas), new { eleccionId = cambios.EleccionId });
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
            var resp = await client.DeleteAsync($"{_endpointListas}/{id}");

            if (resp.IsSuccessStatusCode)
                TempData["Mensaje"] = "Lista eliminada.";
            else
                TempData["Error"] = await resp.Content.ReadAsStringAsync();

            return RedirectToAction(nameof(GestionListas), new { eleccionId });
        }

        // CANDIDATOS

        [HttpGet]
        public async Task<IActionResult> GestionCandidatos(int eleccionId)
        {
            if (eleccionId <= 0)
            {
                TempData["Error"] = "Elección no válida.";
                return RedirectToAction(nameof(GestionElecciones));
            }

            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");

            var respElec = await client.GetAsync($"{_endpointElecciones}/{eleccionId}");
            ViewBag.Eleccion = respElec.IsSuccessStatusCode
                ? await respElec.Content.ReadFromJsonAsync<Eleccion>()
                : null;

            var respListas = await client.GetAsync($"{_endpointListas}/PorEleccion/{eleccionId}");
            ViewBag.Listas = respListas.IsSuccessStatusCode
                ? await respListas.Content.ReadFromJsonAsync<List<Lista>>() ?? new List<Lista>()
                : new List<Lista>();

            var resp = await client.GetAsync($"{_endpointCandidatos}/PorEleccion/{eleccionId}");
            var candidatos = resp.IsSuccessStatusCode
                ? await resp.Content.ReadFromJsonAsync<List<Candidato>>() ?? new List<Candidato>()
                : new List<Candidato>();

            if (!resp.IsSuccessStatusCode)
                TempData["Error"] = await resp.Content.ReadAsStringAsync();

            return View(candidatos);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearCandidato(Candidato candidato)
        {
            if (candidato == null || candidato.EleccionId <= 0)
            {
                TempData["Error"] = "Datos inválidos.";
                return RedirectToAction(nameof(GestionElecciones));
            }

            candidato.Cedula = (candidato.Cedula ?? "").Trim();
            candidato.RolPostulante = (candidato.RolPostulante ?? "").Trim();

            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");
            var resp = await client.PostAsJsonAsync(_endpointCandidatos, candidato);

            if (resp.IsSuccessStatusCode)
                TempData["Mensaje"] = "Candidato registrado.";
            else
                TempData["Error"] = await resp.Content.ReadAsStringAsync();

            return RedirectToAction(nameof(GestionCandidatos), new { eleccionId = candidato.EleccionId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarCandidato(int id, int eleccionId, int listaId, string rolPostulante)
        {
            if (id <= 0 || eleccionId <= 0)
            {
                TempData["Error"] = "Datos inválidos.";
                return RedirectToAction(nameof(GestionElecciones));
            }

            var cambios = new Candidato
            {
                ListaId = listaId,
                RolPostulante = (rolPostulante ?? "").Trim()
            };

            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");
            var resp = await client.PutAsJsonAsync($"{_endpointCandidatos}/{id}", cambios);

            if (resp.IsSuccessStatusCode)
                TempData["Mensaje"] = "Candidato actualizado.";
            else
                TempData["Error"] = await resp.Content.ReadAsStringAsync();

            return RedirectToAction(nameof(GestionCandidatos), new { eleccionId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarCandidato(int id, int eleccionId)
        {
            if (id <= 0 || eleccionId <= 0)
            {
                TempData["Error"] = "Datos inválidos.";
                return RedirectToAction(nameof(GestionElecciones));
            }

            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");
            var resp = await client.DeleteAsync($"{_endpointCandidatos}/{id}");

            if (resp.IsSuccessStatusCode)
                TempData["Mensaje"] = "Candidato eliminado.";
            else
                TempData["Error"] = await resp.Content.ReadAsStringAsync();

            return RedirectToAction(nameof(GestionCandidatos), new { eleccionId });
        }
        [HttpGet]
        public async Task<IActionResult> VerificarJuntas(int? eleccionId)
        {
            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");

            var respElec = await client.GetAsync(_endpointElecciones);
            var elecciones = respElec.IsSuccessStatusCode
                ? await respElec.Content.ReadFromJsonAsync<List<Eleccion>>() ?? new List<Eleccion>()
                : new List<Eleccion>();

            ViewBag.Elecciones = elecciones;

            if (eleccionId == null || eleccionId <= 0)
            {
                var activa = elecciones.FirstOrDefault(e => e.Estado == "ACTIVA");
                eleccionId = activa?.Id ?? elecciones.OrderByDescending(x => x.Id).FirstOrDefault()?.Id ?? 0;
            }

            ViewBag.EleccionId = eleccionId ?? 0;

            if ((eleccionId ?? 0) <= 0)
            {
                TempData["Error"] = "No hay elecciones disponibles.";
                return View(new List<JuntaDetalleDto>());
            }

            var respJuntas = await client.GetAsync($"{_endpointJuntas}/PorEleccion/{eleccionId}");
            var juntas = respJuntas.IsSuccessStatusCode
                ? await respJuntas.Content.ReadFromJsonAsync<List<JuntaDetalleDto>>() ?? new List<JuntaDetalleDto>()
                : new List<JuntaDetalleDto>();

            if (!respJuntas.IsSuccessStatusCode)
                TempData["Error"] = await respJuntas.Content.ReadAsStringAsync();

            return View(juntas);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AprobarJuntaVerificada(long id, int eleccionId)
        {
            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");

            var resp = await client.PutAsync($"{_endpointJuntas}/AprobarJunta/{id}", null);

            if (resp.IsSuccessStatusCode)
                TempData["Mensaje"] = "Junta aprobada.";
            else
                TempData["Error"] = await resp.Content.ReadAsStringAsync();

            return RedirectToAction(nameof(VerificarJuntas), new { eleccionId });
        }

    }
}
