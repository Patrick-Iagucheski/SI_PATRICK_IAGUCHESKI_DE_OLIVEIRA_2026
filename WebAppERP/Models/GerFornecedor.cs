using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAppERP.Models;

[Table("tbGerFornecedores")]
public class GerFornecedor
{
    [Key]
    [Column("idFornecedor")]
    public int IdFornecedor { get; set; }

    [Column("dsRazaoSocial")]
    [Required, MaxLength(200)]
    public string DsRazaoSocial { get; set; } = string.Empty;

    [Column("dsEndereco")]
    [MaxLength(200)]
    public string? DsEndereco { get; set; }

    [Column("dsBairro")]
    [MaxLength(100)]
    public string? DsBairro { get; set; }

    [Column("idCidade")]
    public int? IdCidade { get; set; }

    [ForeignKey("IdCidade")]
    public GerCidade? Cidade { get; set; }

    [Column("nrCEP")]
    [MaxLength(10)]
    public string? NrCEP { get; set; }

    [Column("nrTelefone")]
    [MaxLength(20)]
    public string? NrTelefone { get; set; }

    [Column("dsEmail")]
    [MaxLength(150)]
    public string? DsEmail { get; set; }

    [Column("nrInscEstadual")]
    [MaxLength(20)]
    public string? NrInscEstadual { get; set; }

    [Column("nrInscEstSubTrib")]
    [MaxLength(20)]
    public string? NrInscEstSubTrib { get; set; }

    [Column("nrCNPJ")]
    [Required, MaxLength(18)]
    public string NrCNPJ { get; set; } = string.Empty;

    [Column("dtCriacao")]
    public DateTime DtCriacao { get; set; } = DateTime.Now;

    [Column("idUsuarioEdicao")]
    public int? IdUsuarioEdicao { get; set; }

    [Column("dtEdicao")]
    public DateTime? DtEdicao { get; set; }
}
