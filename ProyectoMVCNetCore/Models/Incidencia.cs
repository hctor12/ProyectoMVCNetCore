using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoMVCNetCore.Models
{
    [Table("INCIDENCIAS")]
    public class Incidencia
    {
        [Key]
        [Column("IDINCIDENCIA")]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int IdIncidencia { get; set; }

        [Column("TITULO")]
        [Required]
        [StringLength(200)]
        public string Titulo { get; set; }

        [Column("DESCRIPCION")]
        [Required]
        public string Descripcion { get; set; }

        [Column("FECHA_REPORTE")]
        public DateTime FechaReporte { get; set; }

        [Column("IDUSUARIO_CLIENTE")]
        public int IdUsuarioCliente { get; set; }

        [Column("IDUSUARIO_TECNICO")]
        public int? IdUsuarioTecnico { get; set; }

        [Column("IDESTADO")]
        public int IdEstado { get; set; }

        [Column("FICHERO")]
        public string Fichero { get; set; }
    }
}
