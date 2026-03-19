using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using ProyectoMVCNetCore.Filters;
using ProyectoMVCNetCore.Models;
using ProyectoMVCNetCore.Repositories;

namespace ProyectoMVCNetCore.Controllers
{
    public class HomeController : Controller
    {
        private IRepositoryIncidencias repo;

        public HomeController(IRepositoryIncidencias repo)
        {
            this.repo = repo;
        }

        [AuthorizeIncidencias]
        public async Task<IActionResult> Index()
        {
            string rol = HttpContext.User.FindFirstValue(ClaimTypes.Role);
            int idUsuario = int.Parse(HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

            // Los usuarios normales no ven el dashboard general, van directo a sus tickets
            if (rol == "Usuario") return RedirectToAction("Index", "Incidencias");

            List<Incidencia> incidencias;
            List<Incidencia> todas = await this.repo.GetIncidenciasAsync();

            if (rol == "Tecnico")
            {
                incidencias = await this.repo.GetIncidenciasTecnicoAsync(idUsuario);
                ViewData["IsTecnico"] = true;
                ViewData["Pendientes"] = todas.Count(i => i.IdEstado == 1);
            }
            else
            {
                incidencias = todas;
                ViewData["IsTecnico"] = false;
                ViewData["Pendientes"] = incidencias.Count(i => i.IdEstado == 1);
            }

            // Calculamos estadísticas básicas basadas en la lista 'incidencias' (que para el técnico son las suyas)
            ViewData["Total"] = incidencias.Count;
            ViewData["EnProceso"] = incidencias.Count(i => i.IdEstado == 2);
            ViewData["Finalizadas"] = incidencias.Count(i => i.IdEstado == 3);

            // Tickets urgentes (los 5 más recientes que no estén finalizados)
            // Para el técnico, mostramos sus propios tickets urgentes
            var urgentes = incidencias.Where(i => i.IdEstado != 3).Take(5).ToList();

            ViewData["Estados"] = await this.repo.GetEstadosAsync();
            ViewData["Tecnicos"] = await this.repo.GetTecnicosAsync();

            return View(urgentes);
        }

        public IActionResult Privacy()
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
