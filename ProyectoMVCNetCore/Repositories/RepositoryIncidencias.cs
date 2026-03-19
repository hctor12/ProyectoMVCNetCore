using Microsoft.EntityFrameworkCore;
using ProyectoMVCNetCore.Data;
using ProyectoMVCNetCore.Helpers;
using ProyectoMVCNetCore.Models;

namespace ProyectoMVCNetCore.Repositories
{
    public class RepositoryIncidencias : IRepositoryIncidencias
    {
        private IncidenciasContext context;

        public RepositoryIncidencias(IncidenciasContext context)
        {
            this.context = context;
        }

        #region Auth
        public async Task RegisterUsuarioAsync(string nombre, string email, string password, string imagen, int idRol)
        {
            // 1. Crear el usuario en USERS
            Usuario user = new Usuario
            {
                IdUsuario = await this.GetMaxIdUsuarioAsync(),
                Nombre = nombre,
                Email = email,
                Imagen = imagen,
                Password = password, // Texto plano para DEV 
                IdRol = idRol
            };
            this.context.Usuarios.Add(user);

            // 2. Generar seguridad en USERS_SECURITY
            string salt = HelperTools.GenerateSalt();
            byte[] passEncrypted = HelperCryptography.EncryptPassword(password, salt);

            UsuarioSecurity security = new UsuarioSecurity
            {
                IdUsuario = user.IdUsuario,
                Salt = salt,
                Pass = passEncrypted
            };
            this.context.UsuariosSecurity.Add(security);

            await this.context.SaveChangesAsync();
        }

        public async Task<Usuario?> LoginUsuarioAsync(string email, string password)
        {
            // Usar la vista de validación
            var vistaUser = await this.context.VistaValidacionUsers
                .FirstOrDefaultAsync(v => v.Email == email);

            if (vistaUser == null) return null;

            // Comparar el password usando salt y Helper
            byte[] passLogin = HelperCryptography.EncryptPassword(password, vistaUser.Salt);
            bool esValido = HelperTools.CompareArrays(passLogin, vistaUser.Pass);

            if (esValido)
            {
                return await this.context.Usuarios
                    .FirstOrDefaultAsync(u => u.Email == email);
            }

            return null;
        }

        public async Task UpdateUserSecurityAsync(int idUsuario, string password)
        {
            string salt = HelperTools.GenerateSalt();
            byte[] passEncrypted = HelperCryptography.EncryptPassword(password, salt);

            var security = await this.context.UsuariosSecurity.FindAsync(idUsuario);
            if (security == null)
            {
                security = new UsuarioSecurity { IdUsuario = idUsuario };
                this.context.UsuariosSecurity.Add(security);
            }

            security.Salt = salt;
            security.Pass = passEncrypted;

            await this.context.SaveChangesAsync();
        }

        private async Task<int> GetMaxIdUsuarioAsync()
        {
            if (await this.context.Usuarios.AnyAsync())
            {
                return await this.context.Usuarios.MaxAsync(u => u.IdUsuario) + 1;
            }
            return 1;
        }
        #endregion

        #region Usuarios
        public async Task<List<Usuario>> GetUsuariosAsync()
        {
            return await this.context.Usuarios.ToListAsync();
        }

        public async Task<List<Usuario>> GetTecnicosAsync()
        {
            return await this.context.Usuarios.Where(u => u.IdRol == 2).ToListAsync();
        }

        public async Task<Usuario?> FindUsuarioAsync(int idUsuario)
        {
            return await this.context.Usuarios.FindAsync(idUsuario);
        }

        public async Task UpdateUsuarioAsync(int idUsuario, string nombre, string email, int idRol, string? imagen)
        {
            Usuario? user = await this.context.Usuarios.FindAsync(idUsuario);
            if (user != null)
            {
                user.Nombre = nombre;
                user.Email = email;
                user.IdRol = idRol;
                if (imagen != null)
                {
                    user.Imagen = imagen;
                }
                await this.context.SaveChangesAsync();
            }
        }

        public async Task UpdateImagenAsync(int idUsuario, string imagen)
        {
            Usuario? user = await this.context.Usuarios.FindAsync(idUsuario);
            if (user != null)
            {
                user.Imagen = imagen;
                await this.context.SaveChangesAsync();
            }
        }

        public async Task DeleteUsuarioAsync(int idUsuario)
        {
            Usuario? user = await this.context.Usuarios.FindAsync(idUsuario);
            if (user != null)
            {
                // Limpiamos dependencias directas para evitar fallos de Foreign Key y el problema de concurrencia (0 affected rows) en EF
                await this.context.Database.ExecuteSqlRawAsync("DELETE FROM COMENTARIOS WHERE IDUSUARIO = {0}", idUsuario);
                await this.context.Database.ExecuteSqlRawAsync("UPDATE INCIDENCIAS SET IDUSUARIO_TECNICO = NULL WHERE IDUSUARIO_TECNICO = {0}", idUsuario);
                await this.context.Database.ExecuteSqlRawAsync("DELETE FROM INCIDENCIAS WHERE IDUSUARIO_CLIENTE = {0}", idUsuario);
                
                await this.context.Database.ExecuteSqlRawAsync("DELETE FROM USERS_SECURITY WHERE IDUSUARIO = {0}", idUsuario);
                await this.context.Database.ExecuteSqlRawAsync("DELETE FROM USERS WHERE IDUSUARIO = {0}", idUsuario);
            }
        }
        #endregion

        #region Incidencias
        public async Task<List<Incidencia>> GetIncidenciasAsync()
        {
            return await this.context.Incidencias.OrderByDescending(i => i.FechaReporte).ToListAsync();
        }

        public async Task<List<Incidencia>> GetIncidenciasUsuarioAsync(int idUsuario)
        {
            return await this.context.Incidencias
                .Where(i => i.IdUsuarioCliente == idUsuario)
                .OrderByDescending(i => i.FechaReporte)
                .ToListAsync();
        }

        public async Task<List<Incidencia>> GetIncidenciasTecnicoAsync(int idTecnico)
        {
            return await this.context.Incidencias
                .Where(i => i.IdUsuarioTecnico == idTecnico)
                .OrderByDescending(i => i.FechaReporte)
                .ToListAsync();
        }

        public async Task<Incidencia?> FindIncidenciaAsync(int idIncidencia)
        {
            return await this.context.Incidencias.FindAsync(idIncidencia);
        }

        public async Task<Incidencia> CreateIncidenciaAsync(string titulo, string descripcion, int idUsuarioCliente, string fichero)
        {
            Incidencia incc = new Incidencia
            {
                IdIncidencia = await this.GetMaxIdIncidenciaAsync(),
                Titulo = titulo,
                Descripcion = descripcion,
                IdUsuarioCliente = idUsuarioCliente,
                FechaReporte = DateTime.Now,
                IdEstado = 1, // Pendiente
                Fichero = fichero
            };
            this.context.Incidencias.Add(incc);
            await this.context.SaveChangesAsync();
            return incc;
        }

        private async Task<int> GetMaxIdIncidenciaAsync()
        {
            if (await this.context.Incidencias.AnyAsync())
            {
                return await this.context.Incidencias.MaxAsync(i => i.IdIncidencia) + 1;
            }
            return 1;
        }

        public async Task AsignarTecnicoAsync(int idIncidencia, int idTecnico)
        {
            var incc = await this.context.Incidencias.FindAsync(idIncidencia);
            if (incc != null)
            {
                incc.IdUsuarioTecnico = idTecnico;
                incc.IdEstado = 2; // En Proceso
                await this.context.SaveChangesAsync();
            }
        }

        public async Task CambiarEstadoIncidenciaAsync(int idIncidencia, int idEstado)
        {
            var incc = await this.context.Incidencias.FindAsync(idIncidencia);
            if (incc != null)
            {
                incc.IdEstado = idEstado;
                await this.context.SaveChangesAsync();
            }
        }
        #endregion

        #region Comentarios
        public async Task<List<Comentario>> GetComentariosIncidenciaAsync(int idIncidencia)
        {
            return await this.context.Comentarios
                .Where(c => c.IdIncidencia == idIncidencia)
                .OrderBy(c => c.Fecha)
                .ToListAsync();
        }

        public async Task AddComentarioAsync(int idIncidencia, int idUsuario, string contenido)
        {
            Comentario com = new Comentario
            {
                IdComentario = await this.GetMaxIdComentarioAsync(),
                IdIncidencia = idIncidencia,
                IdUsuario = idUsuario,
                Contenido = contenido,
                Fecha = DateTime.Now
            };
            this.context.Comentarios.Add(com);
            await this.context.SaveChangesAsync();
        }

        private async Task<int> GetMaxIdComentarioAsync()
        {
            if (await this.context.Comentarios.AnyAsync())
            {
                return await this.context.Comentarios.MaxAsync(c => c.IdComentario) + 1;
            }
            return 1;
        }
        #endregion

        #region Roles & Estados
        public async Task<List<Rol>> GetRolesAsync()
        {
            return await this.context.Roles.ToListAsync();
        }

        public async Task<List<Estado>> GetEstadosAsync()
        {
            return await this.context.Estados.ToListAsync();
        }
        #endregion
    }
}
