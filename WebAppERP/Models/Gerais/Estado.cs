using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAppERP.Models.Gerais;

[Table("tbGerEstados")]
public class Estado
{
    [Key]
    [Column("idEstado")]
    public int IdEstado { get; set; }

    [Column("sgUF")]
    [Required, MaxLength(2)]
    public string SgUF { get; set; } = string.Empty;

    [Column("nmEstado")]
    [Required, MaxLength(100)]
    public string NmEstado { get; set; } = string.Empty;

    [Column("idPais")]
    public int IdPais { get; set; }

    [ForeignKey("IdPais")]
    public Pais? Pais { get; set; }

    [Column("dtCriacao")]
    public DateTime DtCriacao { get; set; } = DateTime.Now;

    [Column("idUsuarioEdicao")]
    public int? IdUsuarioEdicao { get; set; }

    [Column("dtEdicao")]
    public DateTime? DtEdicao { get; set; }
}
