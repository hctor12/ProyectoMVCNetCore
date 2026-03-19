using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using ProyectoMVCNetCore.Filters;
using ProyectoMVCNetCore.Models;
using ProyectoMVCNetCore.Repositories;
using Microsoft.AspNetCore.SignalR;
using ProyectoMVCNetCore.Hubs;

namespace ProyectoMVCNetCore.Controllers
{
    public class IncidenciasController : Controller
    {
        private IRepositoryIncidencias repo;
        private IHubContext<IncidenciasHub> _hubContext;
        private IHubContext<PresenceHub> _presenceContext;

        public IncidenciasController(IRepositoryIncidencias repo, IHubContext<IncidenciasHub> hubContext, IHubContext<PresenceHub> presenceContext)
        {
            this.repo = repo;
            this._hubContext = hubContext;
            this._presenceContext = presenceContext;
        }

        [AuthorizeIncidencias]
        public async Task<IActionResult> Index()
        {
            int idUsuario = int.Parse(HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier));
            int idRol = int.Parse(HttpContext.User.FindFirstValue("IdRol"));

            List<Incidencia> incidencias;

            if (idRol == 1) // Admin
            {
                incidencias = await this.repo.GetIncidenciasAsync();
                ViewData["Tecnicos"] = await this.repo.GetTecnicosAsync();
            }
            else if (idRol == 2) // Técnico
            {
                // Un técnico ve sus tickets Y los que no tienen técnico asignado
                var todas = await this.repo.GetIncidenciasAsync();
                incidencias = todas.Where(i => i.IdUsuarioTecnico == idUsuario || i.IdUsuarioTecnico == null).ToList();
            }
            else // Usuario
            {
                incidencias = await this.repo.GetIncidenciasUsuarioAsync(idUsuario);
            }

            ViewData["Estados"] = await this.repo.GetEstadosAsync();
            return View(incidencias);
        }

        [AuthorizeIncidencias]
        public IActionResult Create()
        {
            return View();
        }

        [AuthorizeIncidencias]
        [HttpPost]
        public async Task<IActionResult> Create(string titulo, string descripcion, IFormFile fichero)
        {
            int idUsuario = int.Parse(HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier));

            string fileName = "";
            if (fichero != null)
            {
                fileName = fichero.FileName;
                string path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", fileName);
                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await fichero.CopyToAsync(stream);
                }
            }

            Incidencia nueva = await this.repo.CreateIncidenciaAsync(titulo, descripcion, idUsuario, fileName);
            await this._presenceContext.Clients.All.SendAsync("RefreshDashboard");
            return RedirectToAction("Details", new { id = nueva.IdIncidencia });
        }

        [AuthorizeIncidencias]
        public async Task<IActionResult> Details(int id)
        {
            Incidencia? incc = await this.repo.FindIncidenciaAsync(id);
            if (incc == null) return NotFound();

            // Guardamos la última fecha de visita en una cookie que dura muchísimo
            CookieOptions options = new CookieOptions { Expires = DateTime.Now.AddYears(1) };
            HttpContext.Response.Cookies.Append($"LastSeen_Incidencia_{id}", DateTime.Now.Ticks.ToString(), options);

            ViewData["Comentarios"] = await this.repo.GetComentariosIncidenciaAsync(id);
            ViewData["Estados"] = await this.repo.GetEstadosAsync();
            ViewData["Usuarios"] = await this.repo.GetUsuariosAsync();

            return View(incc);
        }

        [AuthorizeIncidencias]
        [HttpPost]
        public async Task<IActionResult> AddComentario(int idIncidencia, string contenido)
        {
            int idUsuario = int.Parse(HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier));
            string nombre = HttpContext.User.FindFirstValue(ClaimTypes.Name);
            string imagen = HttpContext.User.FindFirstValue("Imagen");

            await this.repo.AddComentarioAsync(idIncidencia, idUsuario, contenido);

            // Notificar por SignalR (Local en el chat)
            await this._hubContext.Clients.Group(idIncidencia.ToString())
                .SendAsync("ReceiveComment", idUsuario, nombre, imagen, contenido, DateTime.Now.ToString("HH:mm"));

            // Notificación Global
            var currentTicket = await this.repo.FindIncidenciaAsync(idIncidencia);
            if (currentTicket != null)
            {
                int idDestinatario = 0;
                if (currentTicket.IdUsuarioCliente == idUsuario && currentTicket.IdUsuarioTecnico.HasValue)
                {
                    idDestinatario = currentTicket.IdUsuarioTecnico.Value; // Si soy el cliente, mando al técnico
                }
                else if (currentTicket.IdUsuarioTecnico == idUsuario)
                {
                    idDestinatario = currentTicket.IdUsuarioCliente; // Si soy el técnico, mando al cliente
                }

                if (idDestinatario != 0)
                {
                    await this._presenceContext.Clients.User(idDestinatario.ToString())
                        .SendAsync("ReceiveGlobalNotification", nombre, currentTicket.IdIncidencia, currentTicket.Titulo, "comment");
                }
            }

            return Ok();
        }

        [AuthorizeIncidencias(Policy = "AdminOnly")]
        [HttpPost]
        public async Task<IActionResult> AsignarTecnico(int idIncidencia, int idTecnico)
        {
            await this.repo.AsignarTecnicoAsync(idIncidencia, idTecnico);

            // Notificación Global
            var inc = await this.repo.FindIncidenciaAsync(idIncidencia);
            if (inc != null) {
                await this._presenceContext.Clients.User(idTecnico.ToString())
                    .SendAsync("ReceiveGlobalNotification", $"Se te ha asignado el Ticket #{idIncidencia}", inc.IdIncidencia, inc.Titulo, "status");

                // Notificar al cliente
                await this._presenceContext.Clients.User(inc.IdUsuarioCliente.ToString())
                    .SendAsync("ReceiveGlobalNotification", $"Un técnico ha sido asignado a tu ticket {inc.Titulo}", inc.IdIncidencia, inc.Titulo, "status");
            }

            await this._presenceContext.Clients.All.SendAsync("RefreshDashboard");

            return RedirectToAction("Index");
        }

        [AuthorizeIncidencias(Policy = "TecnicoOrAdmin")]
        [HttpPost]
        public async Task<IActionResult> CambiarEstado(int idIncidencia, int idEstado)
        {
            await this.repo.CambiarEstadoIncidenciaAsync(idIncidencia, idEstado);
            
            // Notificar cambio de estado por SignalR
            var estados = await this.repo.GetEstadosAsync();
            var nombreEstado = estados.FirstOrDefault(e => e.IdEstado == idEstado)?.NombreEstado ?? "Desconocido";
            await this._hubContext.Clients.Group(idIncidencia.ToString())
                .SendAsync("ReceiveStatusUpdate", idEstado, nombreEstado);

            // Si pasa a cerrado/finalizado, avisar al cliente
            if (idEstado == 3)
            {
                var inc = await this.repo.FindIncidenciaAsync(idIncidencia);
                if (inc != null)
                {
                    await this._presenceContext.Clients.User(inc.IdUsuarioCliente.ToString())
                        .SendAsync("ReceiveGlobalNotification", $"El técnico ha finalizado tu ticket {inc.Titulo}", inc.IdIncidencia, inc.Titulo, "status");
                }
            }

            await this._presenceContext.Clients.All.SendAsync("RefreshDashboard");

            return RedirectToAction("Details", new { id = idIncidencia });
        }

        [AuthorizeIncidencias]
        [HttpPost]
        public async Task<IActionResult> AtenderTicket(int idIncidencia)
        {
            int idRol = int.Parse(HttpContext.User.FindFirstValue("IdRol"));
            if (idRol != 2) return Forbid();

            Incidencia? inc = await this.repo.FindIncidenciaAsync(idIncidencia);
            if (inc == null) return NotFound();

            if (inc.IdUsuarioTecnico != null || inc.IdEstado == 3)
            {
                return RedirectToAction("Details", new { id = idIncidencia });
            }

            int idUsuario = int.Parse(HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier));
            
            await this.repo.AsignarTecnicoAsync(idIncidencia, idUsuario);
            await this.repo.CambiarEstadoIncidenciaAsync(idIncidencia, 2);

            await this._hubContext.Clients.Group(idIncidencia.ToString())
                .SendAsync("ReceiveStatusUpdate", 2, "En Proceso");

            // Notificar al cliente que su ticket ya lo está atendiendo alguien
            string nombreTecnico = HttpContext.User.FindFirstValue(ClaimTypes.Name) ?? "Un técnico";
            await this._presenceContext.Clients.User(inc.IdUsuarioCliente.ToString())
                .SendAsync("ReceiveGlobalNotification", $"El técnico {nombreTecnico} está atendiendo tu ticket {inc.Titulo}", inc.IdIncidencia, inc.Titulo, "status");

            await this._presenceContext.Clients.All.SendAsync("RefreshDashboard");

            return RedirectToAction("Details", new { id = idIncidencia });
        }

        [AuthorizeIncidencias]
        [HttpPost]
        public async Task<IActionResult> CerrarTicket(int idIncidencia)
        {
            Incidencia? inc = await this.repo.FindIncidenciaAsync(idIncidencia);
            if (inc == null) return NotFound();

            int idUsuario = int.Parse(HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier));
            
            // Solo el dueño puede cerrar
            if (inc.IdUsuarioCliente != idUsuario) return Forbid();

            // Cambiar a "Finalizado" (Estado 3)
            await this.repo.CambiarEstadoIncidenciaAsync(idIncidencia, 3);

            // Notificar por SignalR
            await this._hubContext.Clients.Group(idIncidencia.ToString())
                .SendAsync("ReceiveStatusUpdate", 3, "Finalizado");

            // Notificación Global para el técnico (si tiene)
            if (inc.IdUsuarioTecnico.HasValue)
            {
                string nombreCliente = HttpContext.User.FindFirstValue(ClaimTypes.Name) ?? "El Cliente";
                await this._presenceContext.Clients.User(inc.IdUsuarioTecnico.Value.ToString())
                    .SendAsync("ReceiveGlobalNotification", $"El ticket {inc.Titulo} ha sido cerrado por {nombreCliente}", inc.IdIncidencia, inc.Titulo, "status");
            }

            await this._presenceContext.Clients.All.SendAsync("RefreshDashboard");

            return RedirectToAction("Details", new { id = idIncidencia });
        }

        [AuthorizeIncidencias]
        public async Task<IActionResult> DescargarResumen(int id)
        {
            Incidencia? inc = await this.repo.FindIncidenciaAsync(id);
            if (inc == null) return NotFound();

            int idUsuario = int.Parse(HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier));
            int idRol = int.Parse(HttpContext.User.FindFirstValue("IdRol"));

            // Validar acceso: Cliente del ticket o Técnico asignado (o Admin)
            bool esDuenio = inc.IdUsuarioCliente == idUsuario;
            bool esTecnicoAsignado = inc.IdUsuarioTecnico == idUsuario;
            bool esAdmin = idRol == 1;

            if (!esDuenio && !esTecnicoAsignado && !esAdmin)
            {
                return Forbid();
            }

            // Validar que esté finalizado (o permitir a admin/tecnico descargar aunque no esté finalizado si así se prefiere, pero el usuario pidió "cuando se finalice")
            // Dejaré que se descargue si está finalizado (Estado 3)
            if (inc.IdEstado != 3 && !esAdmin)
            {
                return RedirectToAction("Details", new { id = id });
            }

            var comentarios = await this.repo.GetComentariosIncidenciaAsync(id);
            var cliente = await this.repo.FindUsuarioAsync(inc.IdUsuarioCliente);
            var tecnico = inc.IdUsuarioTecnico.HasValue ? await this.repo.FindUsuarioAsync(inc.IdUsuarioTecnico.Value) : null;
            var estados = await this.repo.GetEstadosAsync();
            var nombreEstado = estados.FirstOrDefault(e => e.IdEstado == inc.IdEstado)?.NombreEstado ?? "Finalizado";

            byte[] pdf = ProyectoMVCNetCore.Helpers.HelperQuestPDF.GenerateTicketSummary(inc, comentarios, cliente!, tecnico, nombreEstado);

            string fileName = $"Resumen_Ticket_{inc.FechaReporte:yyyyMMdd_HHmm}.pdf";
            return File(pdf, "application/pdf", fileName);
        }

        [HttpPost]
        [AuthorizeIncidencias]
        public async Task<IActionResult> LimpiarNotificaciones()
        {
            int idUsuario = int.Parse(HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier));
            int idRol = int.Parse(HttpContext.User.FindFirstValue("IdRol"));

            var incidencias = idRol == 2 || idRol == 1
                ? (await repo.GetIncidenciasAsync()).Where(i => i.IdUsuarioTecnico == idUsuario || (idRol == 1)).ToList()
                : await repo.GetIncidenciasUsuarioAsync(idUsuario);

            CookieOptions options = new CookieOptions { Expires = DateTime.Now.AddYears(1) };
            string ticks = DateTime.Now.Ticks.ToString();

            foreach(var i in incidencias) {
                HttpContext.Response.Cookies.Append($"LastSeen_Incidencia_{i.IdIncidencia}", ticks, options);
            }

            return Ok();
        }
    }
}
