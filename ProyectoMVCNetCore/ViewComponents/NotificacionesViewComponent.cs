using Microsoft.AspNetCore.Mvc;
using ProyectoMVCNetCore.Repositories;
using System.Security.Claims;

namespace ProyectoMVCNetCore.ViewComponents
{
    public class NotificacionesViewComponent : ViewComponent
    {
        private IRepositoryIncidencias repo;

        public NotificacionesViewComponent(IRepositoryIncidencias repo)
        {
            this.repo = repo;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
            {
                return View(0);
            }

            int idUsuario = int.Parse(HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier));
            int idRol = int.Parse(HttpContext.User.FindFirstValue("IdRol"));

            var notificaciones = new List<Tuple<ProyectoMVCNetCore.Models.Incidencia, string, string, int>>();

            // Los administradores no reciben notificaciones por ahora
            if (idRol == 1)
            {
                return View(notificaciones);
            }

            var incidencias = idRol == 2
                ? (await repo.GetIncidenciasAsync()).Where(i => i.IdUsuarioTecnico == idUsuario).ToList()
                : await repo.GetIncidenciasUsuarioAsync(idUsuario);

            foreach (var inc in incidencias)
            {
                var cookieValue = HttpContext.Request.Cookies[$"LastSeen_Incidencia_{inc.IdIncidencia}"];
                DateTime lastSeen = DateTime.MinValue;

                if (!string.IsNullOrEmpty(cookieValue) && long.TryParse(cookieValue, out long ticks))
                {
                    lastSeen = new DateTime(ticks);
                }

                var comentarios = await repo.GetComentariosIncidenciaAsync(inc.IdIncidencia);
                var ultimo = comentarios.OrderByDescending(c => c.Fecha).FirstOrDefault();

                // Si estamos viendo este ticket justo ahora, lo descartamos de notificaciones (leído enseguida)
                var currentAction = HttpContext.Request.RouteValues["action"]?.ToString();
                var currentController = HttpContext.Request.RouteValues["controller"]?.ToString();
                var currentId = HttpContext.Request.RouteValues["id"]?.ToString();
                
                if (currentController == "Incidencias" && currentAction == "Details" && currentId == inc.IdIncidencia.ToString())
                {
                    continue;
                }

                var noLeidos = comentarios.Where(c => c.Fecha > lastSeen && c.IdUsuario != idUsuario).ToList();

                if (noLeidos.Any())
                {
                    var ultimoCom = noLeidos.OrderByDescending(c => c.Fecha).First();
                    var usuarios = await repo.GetUsuariosAsync();
                    var autor = usuarios.FirstOrDefault(u => u.IdUsuario == ultimoCom.IdUsuario);
                    string nombreAutor = autor?.Nombre ?? "Alguien";

                    string msj = noLeidos.Count > 1 
                        ? $"Tienes nuevos mensajes de {nombreAutor}"
                        : $"Tienes un nuevo mensaje de {nombreAutor}";

                    notificaciones.Add(new Tuple<ProyectoMVCNetCore.Models.Incidencia, string, string, int>(inc, msj, "comment", noLeidos.Count));
                }
                else if (lastSeen == DateTime.MinValue && idRol == 2 && inc.IdEstado == 1)
                {
                    if (inc.IdUsuarioTecnico == idUsuario)
                    {
                        notificaciones.Add(new Tuple<ProyectoMVCNetCore.Models.Incidencia, string, string, int>(inc, "Te acaban de asignar este nuevo ticket.", "status", 0));
                    }
                }
            }

            return View(notificaciones);
        }
    }
}
