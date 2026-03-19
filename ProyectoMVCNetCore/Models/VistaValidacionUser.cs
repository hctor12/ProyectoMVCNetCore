using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoMVCNetCore.Models
{
    [Table("V_VALIDACION_USERS")]
    public class VistaValidacionUser
    {
        [Key]
        [Column("IDUSUARIO")]
        public int IdUsuario { get; set; }

        [Column("EMAIL")]
        public string Email { get; set; }

        [Column("NOMBRE")]
        public string Nombre { get; set; }

        [Column("SALT")]
        public string Salt { get; set; }

        [Column("PASS")]
        public byte[] Pass { get; set; }

        [Column("IDROL")]
        public int IdRol { get; set; }
    }
}
