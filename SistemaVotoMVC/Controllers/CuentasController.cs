using Microsoft.AspNetCore.Mvc;
using SistemaVotoModelos; 
namespace SistemaVotoMVC.Controllers
{
    public class CuentasController : Controller
    {
        public IActionResult Ingreso()
        {
            return View();
        }

        public IActionResult Registro()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Registro(Votante votante)
        {
            votante.RolId = 2; // Asumiendo que 2 es "Votante"
            votante.Estado = true; // Activo por defecto

            if (ModelState.IsValid)
            {
                // Aquí irá la conexión a tu API VotantesController
                return RedirectToAction("Registro");
            }
            return View(votante);
        }
    }
}