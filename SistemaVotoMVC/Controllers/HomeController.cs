using Microsoft.AspNetCore.Mvc;
using SistemaVotoMVC.Models;
using System.Diagnostics;

namespace SistemaVotoMVC.Controllers
{
    public class HomeController : Controller
    {
        // Acción para la página de inicio (Landing Page)
        public IActionResult Index()
        {
            return View();
        }

        // Acción para la página de privacidad
        public IActionResult Privacy()
        {
            return View();
        }

        // Acción para la página de selección (¿Login o Registro?)
        public IActionResult Acceder()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}