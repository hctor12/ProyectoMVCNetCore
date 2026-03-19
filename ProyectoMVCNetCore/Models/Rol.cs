using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoMVCNetCore.Models
{
    [Table("ROLES")]
    public class Rol
    {
        [Key]
        [Column("IDROL")]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int IdRol { get; set; }

        [Column("NOMBREROL")]
        [Required]
        [StringLength(50)]
        public string NombreRol { get; set; }
    }
}
