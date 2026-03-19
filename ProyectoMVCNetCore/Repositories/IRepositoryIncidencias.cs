using ProyectoMVCNetCore.Models;

namespace ProyectoMVCNetCore.Repositories
{
    public interface IRepositoryIncidencias
    {
        // Auth
        Task RegisterUsuarioAsync(string nombre, string email, string password, string imagen, int idRol = 3);
        Task<Usuario?> LoginUsuarioAsync(string email, string password);
        Task UpdateUserSecurityAsync(int idUsuario, string password); // For mass encryption or user password change

        // Usuarios
        Task<List<Usuario>> GetUsuariosAsync();
        Task<List<Usuario>> GetTecnicosAsync();
        Task<Usuario?> FindUsuarioAsync(int idUsuario);
        Task UpdateUsuarioAsync(int idUsuario, string nombre, string email, int idRol, string? imagen);
        Task UpdateImagenAsync(int idUsuario, string imagen);
        Task DeleteUsuarioAsync(int idUsuario);

        // Incidencias
        Task<List<Incidencia>> GetIncidenciasAsync();
        Task<List<Incidencia>> GetIncidenciasUsuarioAsync(int idUsuario);
        Task<List<Incidencia>> GetIncidenciasTecnicoAsync(int idTecnico);
        Task<Incidencia?> FindIncidenciaAsync(int idIncidencia);
        Task<Incidencia> CreateIncidenciaAsync(string titulo, string descripcion, int idUsuarioCliente, string fichero);
        Task AsignarTecnicoAsync(int idIncidencia, int idTecnico);
        Task CambiarEstadoIncidenciaAsync(int idIncidencia, int idEstado);

        // Comentarios
        Task<List<Comentario>> GetComentariosIncidenciaAsync(int idIncidencia);
        Task AddComentarioAsync(int idIncidencia, int idUsuario, string contenido);

        // Roles & Estados
        Task<List<Rol>> GetRolesAsync();
        Task<List<Estado>> GetEstadosAsync();
    }
}
