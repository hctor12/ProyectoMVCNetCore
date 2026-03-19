using Microsoft.EntityFrameworkCore;
using ProyectoMVCNetCore.Models;

namespace ProyectoMVCNetCore.Data
{
    public class IncidenciasContext : DbContext
    {
        public IncidenciasContext(DbContextOptions<IncidenciasContext> options)
            : base(options)
        {
        }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<UsuarioSecurity> UsuariosSecurity { get; set; }
        public DbSet<Rol> Roles { get; set; }
        public DbSet<Incidencia> Incidencias { get; set; }
        public DbSet<Estado> Estados { get; set; }
        public DbSet<Comentario> Comentarios { get; set; }
        public DbSet<VistaValidacionUser> VistaValidacionUsers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<VistaValidacionUser>().HasNoKey();
        }
    }
}
