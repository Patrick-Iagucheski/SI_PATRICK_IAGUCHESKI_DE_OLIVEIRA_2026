using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAppERP.Models;

[Table("tbGerFuncionarios")]
public class GerFuncionario
{
    [Key]
    [Column("idFuncionario")]
    public int IdFuncionario { get; set; }

    [Column("nmFuncionario")]
    [Required, MaxLength(200)]
    public string NmFuncionario { get; set; } = string.Empty;

    [Column("nmApelido")]
    [MaxLength(100)]
    public string? NmApelido { get; set; }

    [Column("nrCpf")]
    [Required, MaxLength(14)]
    public string NrCpf { get; set; } = string.Empty;

    [Column("nrRG")]
    [MaxLength(9)]
    public string? NrRG { get; set; }

    [Column("dsSexo")]
    [MaxLength(30)]
    public string? DsSexo { get; set; }

    [Column("nrTelefone")]
    [MaxLength(20)]
    public string? NrTelefone { get; set; }

    [Column("dsEmail")]
    [MaxLength(150)]
    public string? DsEmail { get; set; }

    [Column("dsEndereco")]
    [MaxLength(200)]
    public string? DsEndereco { get; set; }

    [Column("dsBairro")]
    [MaxLength(100)]
    public string? DsBairro { get; set; }

    [Column("nrNumero")]
    [MaxLength(10)]
    public string? NrNumero { get; set; }

    [Column("dsComplemento")]
    [MaxLength(100)]
    public string? DsComplemento { get; set; }

    [Column("idCidade")]
    public int? IdCidade { get; set; }

    [ForeignKey("IdCidade")]
    public GerCidade? Cidade { get; set; }

    [Column("nrCEP")]
    [MaxLength(10)]
    public string? NrCEP { get; set; }

    [Column("dtNascimento")]
    public DateOnly? DtNascimento { get; set; }

    [Column("dtAdmissao")]
    public DateOnly DtAdmissao { get; set; }

    [Column("dtDemissao")]
    public DateOnly? DtDemissao { get; set; }

    [Column("dsCargo")]
    [Required, MaxLength(100)]
    public string DsCargo { get; set; } = string.Empty;

    // Novo vinculo com a tabela de Cargos (tbGerCargos).
    [Column("idCargo")]
    public int? IdCargo { get; set; }

    [ForeignKey("IdCargo")]
    public GerCargo? Cargo { get; set; }

    [Column("vlSalario")]
    public decimal? VlSalario { get; set; }

    [Column("vlComissao")]
    public decimal? VlComissao { get; set; }

    [Column("dsObservacao")]
    [MaxLength(500)]
    public string? DsObservacao { get; set; }

    [Column("flAtivo")]
    public bool FlAtivo { get; set; } = true;

    [Column("idUsuario")]
    public int? IdUsuario { get; set; }

    [ForeignKey("IdUsuario")]
    public GerUsuario? Usuario { get; set; }

    [Column("dtCriacao")]
    public DateTime DtCriacao { get; set; } = DateTime.Now;

    [Column("idUsuarioEdicao")]
    public int? IdUsuarioEdicao { get; set; }

    [Column("dtEdicao")]
    public DateTime? DtEdicao { get; set; }
}
