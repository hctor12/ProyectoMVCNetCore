using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoMVCNetCore.Models
{
    [Table("USERS")]
    public class Usuario
    {
        [Key]
        [Column("IDUSUARIO")]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int IdUsuario { get; set; }

        [Column("NOMBRE")]
        [Required]
        [StringLength(100)]
        public string Nombre { get; set; }

        [Column("EMAIL")]
        [Required]
        [StringLength(150)]
        public string Email { get; set; }

        [Column("IMAGEN")]
        public string Imagen { get; set; }

        [Column("PASSWORD")]
        public string Password { get; set; }

        [Column("IDROL")]
        public int IdRol { get; set; }
    }
}
