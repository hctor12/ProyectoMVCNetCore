using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoMVCNetCore.Models
{
    [Table("COMENTARIOS")]
    public class Comentario
    {
        [Key]
        [Column("IDCOMENTARIO")]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int IdComentario { get; set; }

        [Column("IDINCIDENCIA")]
        public int IdIncidencia { get; set; }

        [Column("IDUSUARIO")]
        public int IdUsuario { get; set; }

        [Column("CONTENIDO")]
        [Required]
        public string Contenido { get; set; }

        [Column("FECHA")]
        public DateTime Fecha { get; set; }
    }
}
