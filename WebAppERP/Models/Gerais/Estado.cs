using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAppERP.Models.Gerais;

// Cadastro: texto gravado em MAIUSCULO (CaixaAltaAttribute).
[CaixaAlta]
[Table("tbGerEstados")]
public class Estado : EntidadeComRegistro
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
}
