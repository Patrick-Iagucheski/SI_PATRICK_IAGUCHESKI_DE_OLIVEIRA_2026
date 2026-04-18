using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAppERP.Models;

[Table("tbGerServicos")]
public class GerServico
{
    [Key]
    [Column("idServico")]
    public int IdServico { get; set; }

    [Column("dsServico")]
    [Required, MaxLength(200)]
    public string DsServico { get; set; } = string.Empty;

    [Column("dsDescricao")]
    [MaxLength(500)]
    public string? DsDescricao { get; set; }

    [Column("vlServico")]
    public decimal VlServico { get; set; }

    [Column("nrDuracaoMin")]
    public int? NrDuracaoMin { get; set; }

    [Column("dsCategoria")]
    [MaxLength(100)]
    public string? DsCategoria { get; set; }

    [Column("flAtivo")]
    public bool FlAtivo { get; set; } = true;

    [Column("dtCriacao")]
    public DateTime DtCriacao { get; set; } = DateTime.Now;

    [Column("idUsuarioEdicao")]
    public int? IdUsuarioEdicao { get; set; }

    [Column("dtEdicao")]
    public DateTime? DtEdicao { get; set; }
}
