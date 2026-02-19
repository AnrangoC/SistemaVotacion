using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaVotoModelos;
using System.Net.Http.Json;

namespace SistemaVotoMVC.Controllers
{
    public class VotacionController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public VotacionController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View(new VotacionIngresoVm());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(VotacionIngresoVm vm)
        {
            vm.Cedula = (vm.Cedula ?? "").Trim();
            vm.Token = (vm.Token ?? "").Trim();

            if (string.IsNullOrWhiteSpace(vm.Cedula) || string.IsNullOrWhiteSpace(vm.Token))
            {
                TempData["Error"] = "Debes ingresar cédula y token.";
                return View(vm);
            }

            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");


            // 2) Traer elección activa
            var elecResp = await client.GetAsync("api/Elecciones/Activa");
            if (!elecResp.IsSuccessStatusCode)
            {
                var msg = await elecResp.Content.ReadAsStringAsync();
                TempData["Error"] = string.IsNullOrWhiteSpace(msg) ? "No hay elección activa." : msg;
                return View(vm);
            }

            var eleccion = await elecResp.Content.ReadFromJsonAsync<Eleccion>();
            if (eleccion == null)
            {
                TempData["Error"] = "No se pudo leer la elección activa.";
                return View(vm);
            }

            // 3) Traer candidatos
            var candResp = await client.GetAsync($"api/Candidatos/PorEleccion/{eleccion.Id}");
            if (!candResp.IsSuccessStatusCode)
            {
                var msg = await candResp.Content.ReadAsStringAsync();
                TempData["Error"] = string.IsNullOrWhiteSpace(msg) ? "No se pudo obtener candidatos." : msg;
                return View(vm);
            }

            var candidatos = await candResp.Content.ReadFromJsonAsync<List<Candidato>>() ?? new List<Candidato>();

            if (candidatos.Count == 0)
            {
                TempData["Error"] = "La elección está activa pero no hay candidatos registrados.";
                return View(vm);
            }

            var roles = candidatos
                .Select(c => (c.RolPostulante ?? "").Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            var papeleta = new PapeletaNominalVm
            {
                Cedula = vm.Cedula,
                Token = vm.Token,
                EleccionId = eleccion.Id,
                TituloEleccion = eleccion.Titulo ?? "Elección",
                Candidatos = candidatos,
                Roles = roles,
                RolSeleccionado = roles.FirstOrDefault() ?? ""
            };

            return View("PapeletaNominal", papeleta);
        }

        // Helper: recargar papeleta en POST (porque vm.Candidatos no vuelve)
        private async Task<PapeletaNominalVm?> CargarPapeleta(string cedula, string token, int eleccionId, string rolSeleccionado)
        {
            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");

            // Traer elección por id (opcional, pero sirve para título)
            var elec = await client.GetFromJsonAsync<Eleccion>($"api/Elecciones/{eleccionId}");
            if (elec == null) return null;

            var candidatos = await client.GetFromJsonAsync<List<Candidato>>($"api/Candidatos/PorEleccion/{eleccionId}")
                            ?? new List<Candidato>();

            var roles = candidatos
                .Select(c => (c.RolPostulante ?? "").Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            var rolFinal = (rolSeleccionado ?? "").Trim();
            if (string.IsNullOrWhiteSpace(rolFinal))
                rolFinal = roles.FirstOrDefault() ?? "";

            return new PapeletaNominalVm
            {
                Cedula = cedula,
                Token = token,
                EleccionId = eleccionId,
                TituloEleccion = elec.Titulo ?? "Elección",
                Candidatos = candidatos,
                Roles = roles,
                RolSeleccionado = rolFinal
            };
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EmitirNominal(PapeletaNominalVm vm)
        {
            vm.Cedula = (vm.Cedula ?? "").Trim();
            vm.Token = (vm.Token ?? "").Trim();

            if (string.IsNullOrWhiteSpace(vm.Cedula) ||
                string.IsNullOrWhiteSpace(vm.Token) ||
                vm.EleccionId <= 0)
            {
                TempData["Error"] = "Sesión inválida. Vuelve a ingresar.";
                return RedirectToAction(nameof(Index));
            }

            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");

            // Volver a cargar candidatos (POST pierde listas)
            var elec = await client.GetFromJsonAsync<Eleccion>($"api/Elecciones/{vm.EleccionId}");
            if (elec == null)
            {
                TempData["Error"] = "No se pudo cargar la elección.";
                return RedirectToAction(nameof(Index));
            }

            var candidatos = await client.GetFromJsonAsync<List<Candidato>>(
                $"api/Candidatos/PorEleccion/{vm.EleccionId}") ?? new List<Candidato>();

            var roles = candidatos
                .Select(c => (c.RolPostulante ?? "").Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            // AQUÍ ARMAMOS LISTA DE VOTOS
            var votos = new List<object>();

            foreach (var rol in roles)
            {
                string campo = $"voto_{rol}";
                var valor = Request.Form[campo].FirstOrDefault();

                int candidatoId = 0;
                int.TryParse(valor, out candidatoId);

                if (candidatoId == 0)
                {
                    // VOTO EN BLANCO
                    votos.Add(new
                    {
                        RolPostulante = rol,
                        ListaId = 0,
                        CedulaCandidato = ""
                    });
                }
                else
                {
                    var elegido = candidatos.FirstOrDefault(x => x.Id == candidatoId);

                    if (elegido != null)
                    {
                        votos.Add(new
                        {
                            RolPostulante = rol,
                            ListaId = elegido.ListaId,
                            CedulaCandidato = elegido.Cedula
                        });
                    }
                }
            }
            // VALIDAR TOKEN JUSTO AL CONFIRMAR
            var validarResp = await client.PostAsJsonAsync("api/Aut/ValidarToken", new
            {
                cedula = vm.Cedula,
                codigo = vm.Token
            });

            if (!validarResp.IsSuccessStatusCode)
            {
                var msg = await validarResp.Content.ReadAsStringAsync();
                TempData["Error"] = string.IsNullOrWhiteSpace(msg) ? "Token inválido, usado o expirado." : msg;
                return RedirectToAction(nameof(Index));
            }
            // ENVIAMOS TODO JUNTO
            var resp = await client.PostAsJsonAsync("api/VotosAnonimos/EmitirVotacion", new
            {
                Cedula = vm.Cedula,
                EleccionId = vm.EleccionId,
                Votos = votos
            });

            if (!resp.IsSuccessStatusCode)
            {
                TempData["Error"] = await resp.Content.ReadAsStringAsync();
                return RedirectToAction(nameof(Index));
            }

            TempData["Mensaje"] = "Votos registrados correctamente.";
            return RedirectToAction(nameof(Confirmacion));
        }



        [HttpGet]
        public IActionResult Confirmacion()
        {
            return View();
        }
    }

    public class VotacionIngresoVm
    {
        public string Cedula { get; set; } = "";
        public string Token { get; set; } = "";
    }

    public class PapeletaNominalVm
    {
        public string Cedula { get; set; } = "";
        public string Token { get; set; } = "";
        public int EleccionId { get; set; }
        public string TituloEleccion { get; set; } = "";

        public List<Candidato> Candidatos { get; set; } = new();
        public List<string> Roles { get; set; } = new();
        public string RolSeleccionado { get; set; } = "";
    }
}
