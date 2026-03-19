using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using ProyectoMVCNetCore.Models;
using ProyectoMVCNetCore.Repositories;
using System.Security.Claims;

namespace ProyectoMVCNetCore.Controllers
{
    public class AuthController : Controller
    {
        private IRepositoryIncidencias repo;

        public AuthController(IRepositoryIncidencias repo)
        {
            this.repo = repo;
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            Usuario? user = await this.repo.LoginUsuarioAsync(email, password);
            if (user != null)
            {
                ClaimsIdentity identity = new ClaimsIdentity(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    ClaimTypes.Name, ClaimTypes.Role);

                // Claims obligatorios
                identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.IdUsuario.ToString()));
                identity.AddClaim(new Claim(ClaimTypes.Name, user.Nombre));
                identity.AddClaim(new Claim(ClaimTypes.Email, user.Email));
                identity.AddClaim(new Claim(ClaimTypes.Role,
                    user.IdRol == 1 ? "Admin" : user.IdRol == 2 ? "Tecnico" : "Usuario"));

                // Claims personalizados
                identity.AddClaim(new Claim("Imagen", user.Imagen ?? "default.png"));
                identity.AddClaim(new Claim("IdRol", user.IdRol.ToString()));

                ClaimsPrincipal userPrincipal = new ClaimsPrincipal(identity);
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme, userPrincipal);

                // Redirigir a donde intentaba ir (TempData) o al Index
                string controller = TempData["controller"]?.ToString() ?? "Home";
                string action = TempData["action"]?.ToString() ?? "Index";
                if (TempData["id"] != null)
                {
                    string id = TempData["id"].ToString();
                    return RedirectToAction(action, controller, new { id = id });
                }
                else
                {
                    return RedirectToAction(action, controller);
                }
            }
            ViewData["Mensaje"] = "Email o Password incorrectos";
            return View();
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(string nombre, string email, string password, IFormFile imagen)
        {
            string fileName = "default.png";
            if (imagen != null)
            {
                fileName = imagen.FileName;
                string path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", fileName);

                // Asegurar directorio
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await imagen.CopyToAsync(stream);
                }
            }

            await this.repo.RegisterUsuarioAsync(nombre, email, password, fileName);
            return RedirectToAction("Login");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        public IActionResult AccesoDenegado()
        {
            return View();
        }
    }
}
