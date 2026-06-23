using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAppERP.Models;

[Table("tbGerFornecedores")]
public class GerFornecedor
{
    [Key]
    [Column("idFornecedor")]
    public int IdFornecedor { get; set; }

    // ===== Dados Cadastrais =====

    // Tipo de pessoa: F = Fisica (CPF), J = Juridica (CNPJ), E = Estrangeiro, O = Outro
    [Column("tpPessoa")]
    [Required, MaxLength(1)]
    public string TpPessoa { get; set; } = "J";

    [Column("dsRazaoSocial")]
    [Required, MaxLength(200)]
    public string DsRazaoSocial { get; set; } = string.Empty;

    [Column("dsNomeFantasia")]
    [MaxLength(150)]
    public string? DsNomeFantasia { get; set; }

    [Column("dsContatoPrincipal")]
    [MaxLength(120)]
    public string? DsContatoPrincipal { get; set; }

    [Column("nrCpf")]
    [MaxLength(11)]
    public string? NrCpf { get; set; }

    [Column("nrCNPJ")]
    [MaxLength(14)]
    public string? NrCNPJ { get; set; }

    // Documento internacional (Estrangeiro) ou documento livre (Outro)
    [Column("nrDocumento")]
    [MaxLength(30)]
    public string? NrDocumento { get; set; }

    [Column("nrInscEstadual")]
    [MaxLength(20)]
    public string? NrInscEstadual { get; set; }

    [Column("nrInscMunicipal")]
    [MaxLength(20)]
    public string? NrInscMunicipal { get; set; }

    [Column("dsEmail")]
    [MaxLength(150)]
    public string? DsEmail { get; set; }

    [Column("dsObservacao")]
    [MaxLength(500)]
    public string? DsObservacao { get; set; }

    [Column("flAtivo")]
    public bool FlAtivo { get; set; } = true;

    // ===== Endereco =====

    [Column("nrCEP")]
    [MaxLength(8)]
    public string? NrCEP { get; set; }

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

    // ===== Contato =====

    [Column("nrTelefoneFixo")]
    [MaxLength(11)]
    public string? NrTelefoneFixo { get; set; }

    [Column("nrCelular")]
    [MaxLength(11)]
    public string? NrCelular { get; set; }

    [Column("nrWhatsApp")]
    [MaxLength(11)]
    public string? NrWhatsApp { get; set; }

    // ===== Dados Bancarios =====

    [Column("dsBanco")]
    [MaxLength(60)]
    public string? DsBanco { get; set; }

    [Column("nrAgencia")]
    [MaxLength(10)]
    public string? NrAgencia { get; set; }

    // CC = Conta Corrente, CP = Poupanca, CS = Conta Salario, CE = Conta Empresarial
    [Column("tpConta")]
    [MaxLength(2)]
    public string? TpConta { get; set; }

    [Column("nrConta")]
    [MaxLength(20)]
    public string? NrConta { get; set; }

    [Column("nrDigitoConta")]
    [MaxLength(2)]
    public string? NrDigitoConta { get; set; }

    // ===== PIX =====

    // CPF, CNPJ, EMAIL, CELULAR, ALEATORIA
    [Column("tpChavePix")]
    [MaxLength(12)]
    public string? TpChavePix { get; set; }

    [Column("dsChavePix")]
    [MaxLength(100)]
    public string? DsChavePix { get; set; }

    // ===== Auditoria =====

    [Column("dtCriacao")]
    public DateTime DtCriacao { get; set; } = DateTime.Now;

    [Column("idUsuarioEdicao")]
    public int? IdUsuarioEdicao { get; set; }

    [Column("dtEdicao")]
    public DateTime? DtEdicao { get; set; }
}
