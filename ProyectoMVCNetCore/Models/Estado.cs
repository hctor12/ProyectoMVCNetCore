using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoMVCNetCore.Models
{
    [Table("ESTADOS")]
    public class Estado
    {
        [Key]
        [Column("IDESTADO")]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int IdEstado { get; set; }

        [Column("NOMBREESTADO")]
        [Required]
        [StringLength(50)]
        public string NombreEstado { get; set; }
    }
}
