using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using ProyectoMVCNetCore.Filters;
using ProyectoMVCNetCore.Models;
using ProyectoMVCNetCore.Repositories;

namespace ProyectoMVCNetCore.Controllers
{
    public class UsuariosController : Controller
    {
        private IRepositoryIncidencias repo;

        public UsuariosController(IRepositoryIncidencias repo)
        {
            this.repo = repo;
        }

        [AuthorizeIncidencias(Policy = "AdminOnly")]
        public async Task<IActionResult> Index()
        {
            List<Usuario> usuarios = await this.repo.GetUsuariosAsync();
            return View(usuarios);
        }

        [AuthorizeIncidencias(Policy = "AdminOnly")]
        public async Task<IActionResult> Create()
        {
            ViewData["Roles"] = await this.repo.GetRolesAsync();
            return View();
        }

        [AuthorizeIncidencias(Policy = "AdminOnly")]
        [HttpPost]
        public async Task<IActionResult> Create(string nombre, string email, string password, int idRol, IFormFile imagen)
        {
            string fileName = "default.png";
            if (imagen != null)
            {
                fileName = imagen.FileName;
                string path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", fileName);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await imagen.CopyToAsync(stream);
                }
            }

            await this.repo.RegisterUsuarioAsync(nombre, email, password, fileName, idRol);
            return RedirectToAction("Index");
        }

        [AuthorizeIncidencias(Policy = "AdminOnly")]
        [HttpPost]
        public async Task<IActionResult> MigrarSeguridad()
        {
            // Lógica de cifrado masivo: recorre todos los usuarios y actualiza su security usando el pass en plano
            List<Usuario> usuarios = await this.repo.GetUsuariosAsync();
            foreach (var u in usuarios)
            {
                if (!string.IsNullOrEmpty(u.Password))
                {
                    await this.repo.UpdateUserSecurityAsync(u.IdUsuario, u.Password);
                }
            }

            TempData["Mensaje"] = "Sistema de seguridad actualizado correctamente para todos los usuarios.";
            return RedirectToAction("Index");
        }

        [AuthorizeIncidencias]
        public async Task<IActionResult> Details(int id)
        {
            int currentUserId = int.Parse(HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier));
            int currentUserRol = int.Parse(HttpContext.User.FindFirstValue("IdRol"));

            if (currentUserRol != 1 && currentUserId != id)
            {
                return RedirectToAction("Index", "Home");
            }

            Usuario? user = await this.repo.FindUsuarioAsync(id);
            if (user == null) return NotFound();

            // Calcular métricas reales
            List<Incidencia> incidencias;
            if (user.IdRol == 2) // Técnico
            {
                incidencias = await this.repo.GetIncidenciasTecnicoAsync(id);
            }
            else // Cliente u otro
            {
                incidencias = await this.repo.GetIncidenciasUsuarioAsync(id);
            }

            int totalIncidencias = incidencias.Count;
            int finalizadas = incidencias.Count(i => i.IdEstado == 3);
            int satisfaccion = totalIncidencias > 0 ? (finalizadas * 100 / totalIncidencias) : 0;

            ViewData["TotalIncidencias"] = totalIncidencias;
            ViewData["Satisfaccion"] = satisfaccion;

            return View(user);
        }

        [AuthorizeIncidencias(Policy = "AdminOnly")]
        public async Task<IActionResult> Edit(int id)
        {
            Usuario? u = await this.repo.FindUsuarioAsync(id);
            if (u == null) return NotFound();

            ViewData["Roles"] = await this.repo.GetRolesAsync();
            return View(u);
        }

        [AuthorizeIncidencias(Policy = "AdminOnly")]
        [HttpPost]
        public async Task<IActionResult> Edit(int idUsuario, string nombre, string email, int idRol, IFormFile? imagen)
        {
            string? fileName = null;
            if (imagen != null)
            {
                fileName = imagen.FileName;
                string path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", fileName);
                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await imagen.CopyToAsync(stream);
                }
            }

            await this.repo.UpdateUsuarioAsync(idUsuario, nombre, email, idRol, fileName);
            return RedirectToAction("Index");
        }

        [AuthorizeIncidencias]
        [HttpPost]
        public async Task<IActionResult> UpdateImagen(int idUsuario, IFormFile imagen)
        {
            if (imagen != null)
            {
                string fileName = imagen.FileName;
                string path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", fileName);
                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await imagen.CopyToAsync(stream);
                }
                await this.repo.UpdateImagenAsync(idUsuario, fileName);

                // Refrescar cookie para actualizar el header sin cerrar sesión
                if (User.Identity is ClaimsIdentity identity)
                {
                    var claimImg = identity.FindFirst("Imagen");
                    if (claimImg != null)
                    {
                        identity.RemoveClaim(claimImg);
                    }
                    identity.AddClaim(new Claim("Imagen", fileName));

                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
                }
            }
            return RedirectToAction("Details", new { id = idUsuario });
        }

        [AuthorizeIncidencias]
        [HttpPost]
        public async Task<IActionResult> ChangePassword(int idUsuario, string oldPassword, string newPassword)
        {
            int currentUserId = int.Parse(HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier));
            if (currentUserId != idUsuario) return RedirectToAction("Login", "Auth");

            // Verificar contraseña antigua
            string email = HttpContext.User.FindFirstValue(ClaimTypes.Email);
            Usuario? user = await this.repo.LoginUsuarioAsync(email, oldPassword);
            if (user == null)
            {
                TempData["Error"] = "La contraseña actual no es correcta.";
                return RedirectToAction("Details", new { id = idUsuario });
            }

            // Actualizar a la nueva
            await this.repo.UpdateUserSecurityAsync(idUsuario, newPassword);
            TempData["Mensaje"] = "Contraseña actualizada correctamente.";
            return RedirectToAction("Details", new { id = idUsuario });
        }

        [AuthorizeIncidencias(Policy = "AdminOnly")]
        [HttpPost]
        public async Task<IActionResult> Delete(int idUsuario)
        {
            await this.repo.DeleteUsuarioAsync(idUsuario);
            return RedirectToAction("Index");
        }
    }
}
