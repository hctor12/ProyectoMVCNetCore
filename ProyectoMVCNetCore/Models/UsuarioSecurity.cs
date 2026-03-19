using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoMVCNetCore.Models
{
    [Table("USERS_SECURITY")]
    public class UsuarioSecurity
    {
        [Key]
        [Column("IDUSUARIO")]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int IdUsuario { get; set; }

        [Column("SALT")]
        [Required]
        [StringLength(30)]
        public string Salt { get; set; }

        [Column("PASS")]
        [Required]
        public byte[] Pass { get; set; }
    }
}
