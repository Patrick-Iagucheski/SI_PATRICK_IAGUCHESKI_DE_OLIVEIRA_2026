using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAppERP.Models;

[Table("tbGerClientes")]
public class GerCliente
{
    [Key]
    [Column("idCliente")]
    public int IdCliente { get; set; }

    [Column("nmCliente")]
    [Required, MaxLength(200)]
    public string NmCliente { get; set; } = string.Empty;

    // Tipo de pessoa: F = Fisica (CPF), J = Juridica (CNPJ), E = Estrangeiro/Outro
    [Column("tpPessoa")]
    [Required, MaxLength(1)]
    public string TpPessoa { get; set; } = "F";

    [Column("nrCpf")]
    [MaxLength(11)]
    public string? NrCpf { get; set; }

    [Column("nrCnpj")]
    [MaxLength(14)]
    public string? NrCnpj { get; set; }

    // Documento generico para estrangeiro / outro tipo (sem validacao de CPF/CNPJ)
    [Column("nrDocumento")]
    [MaxLength(20)]
    public string? NrDocumento { get; set; }

    [Column("nrRG")]
    [MaxLength(12)]
    public string? NrRG { get; set; }

    [Column("nrTelefone")]
    [MaxLength(11)]
    public string? NrTelefone { get; set; }

    [Column("dsEmail")]
    [MaxLength(150)]
    public string? DsEmail { get; set; }

    [Column("dsEndereco")]
    [MaxLength(200)]
    public string? DsEndereco { get; set; }

    [Column("nrNumero")]
    [MaxLength(10)]
    public string? NrNumero { get; set; }

    [Column("dsComplemento")]
    [MaxLength(100)]
    public string? DsComplemento { get; set; }

    [Column("dsBairro")]
    [MaxLength(100)]
    public string? DsBairro { get; set; }

    [Column("idCidade")]
    public int? IdCidade { get; set; }

    [ForeignKey("IdCidade")]
    public GerCidade? Cidade { get; set; }

    [Column("nrCEP")]
    [MaxLength(8)]
    public string? NrCEP { get; set; }

    [Column("dtNascimento")]
    public DateOnly? DtNascimento { get; set; }

    [Column("dsSexo")]
    [MaxLength(1)]
    public string? DsSexo { get; set; }

    [Column("dsObservacao")]
    [MaxLength(500)]
    public string? DsObservacao { get; set; }

    [Column("flAtivo")]
    public bool FlAtivo { get; set; } = true;

    [Column("dtCriacao")]
    public DateTime DtCriacao { get; set; } = DateTime.Now;

    [Column("idUsuarioEdicao")]
    public int? IdUsuarioEdicao { get; set; }

    [Column("dtEdicao")]
    public DateTime? DtEdicao { get; set; }
}
